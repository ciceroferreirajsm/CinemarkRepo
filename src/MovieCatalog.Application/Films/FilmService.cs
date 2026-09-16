using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieCatalog.Application.Abstractions;
using MovieCatalog.Application.Common;
using MovieCatalog.Application.Films.Dtos;
using MovieCatalog.Domain.Entities;
using MovieCatalog.Domain.Exceptions;
using MovieCatalog.Domain.Repositories;

namespace MovieCatalog.Application.Films;

public class FilmService : IFilmService
{
    private const string FilmCacheKeyPrefix = "films:";
    private const string FilmListCacheKeyPrefix = "films:list:";

    private readonly IFilmRepository _repository;
    private readonly ICacheService _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<FilmService> _logger;

    public FilmService(
        IFilmRepository repository,
        ICacheService cache,
        IEventPublisher eventPublisher,
        IOptions<CacheOptions> cacheOptions,
        ILogger<FilmService> logger)
    {
        _repository = repository;
        _cache = cache;
        _eventPublisher = eventPublisher;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<FilmResponse> CreateAsync(CreateFilmRequest request, string correlationId, CancellationToken cancellationToken = default)
    {
        if (await _repository.ExistsByTitleAsync(request.Title, excludeId: null, cancellationToken))
        {
            throw new BusinessRuleValidationException($"Já existe um filme cadastrado com o título '{request.Title}'.");
        }

        var now = DateTime.UtcNow;
        var film = new Film
        {
            Title = request.Title,
            Synopsis = request.Synopsis,
            Genre = request.Genre,
            ReleaseDate = request.ReleaseDate,
            DurationMinutes = request.DurationMinutes,
            Rating = request.Rating,
            Active = true,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(film, cancellationToken);
        await _cache.RemoveByPrefixAsync(FilmListCacheKeyPrefix, cancellationToken);

        _logger.LogInformation("Film {FilmId} created. CorrelationId={CorrelationId}", film.Id, correlationId);

        await PublishSafelyAsync(
            () => _eventPublisher.PublishFilmCreatedAsync(film, correlationId, cancellationToken),
            film.Id, "FilmCreated", correlationId);

        return FilmResponse.FromEntity(film);
    }

    public async Task<FilmResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildDetailCacheKey(id);
        var cached = await _cache.GetAsync<FilmResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for film {FilmId}", id);
            return cached;
        }

        var film = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.ForEntity("Filme", id);

        var response = FilmResponse.FromEntity(film);
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(_cacheOptions.FilmDetailTtlMinutes), cancellationToken);
        return response;
    }

    public async Task<PagedResult<FilmResponse>> GetListAsync(FilmListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => query.PageSize
        };
        var normalizedQuery = new FilmListQuery { Genre = query.Genre, Active = query.Active, Page = page, PageSize = pageSize };

        var cacheKey = FilmListCacheKeyPrefix + normalizedQuery.ToCacheKeyFragment();
        var cached = await _cache.GetAsync<PagedResult<FilmResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for film list {CacheKey}", cacheKey);
            return cached;
        }

        var genreFilter = normalizedQuery.Genre;
        var activeFilter = normalizedQuery.Active;
        Expression<Func<Film, bool>> predicate = f =>
            !f.IsDeleted
            && (genreFilter == null || f.Genre == genreFilter)
            && (activeFilter == null || f.Active == activeFilter);

        var items = await _repository.FindAsync(predicate, page, pageSize, cancellationToken);
        var totalCount = await _repository.CountAsync(predicate, cancellationToken);

        var result = new PagedResult<FilmResponse>
        {
            Items = items.Select(FilmResponse.FromEntity).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(_cacheOptions.FilmListTtlMinutes), cancellationToken);
        return result;
    }

    public async Task<FilmResponse> UpdateAsync(string id, UpdateFilmRequest request, string correlationId, CancellationToken cancellationToken = default)
    {
        var film = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.ForEntity("Filme", id);

        if (!string.Equals(film.Title, request.Title, StringComparison.OrdinalIgnoreCase)
            && await _repository.ExistsByTitleAsync(request.Title, excludeId: id, cancellationToken))
        {
            throw new BusinessRuleValidationException($"Já existe um filme cadastrado com o título '{request.Title}'.");
        }

        film.Title = request.Title;
        film.Synopsis = request.Synopsis;
        film.Genre = request.Genre;
        film.ReleaseDate = request.ReleaseDate;
        film.DurationMinutes = request.DurationMinutes;
        film.Rating = request.Rating;
        film.Active = request.Active;
        film.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(id, film, cancellationToken);
        await InvalidateFilmCacheAsync(id, cancellationToken);

        _logger.LogInformation("Film {FilmId} updated. CorrelationId={CorrelationId}", id, correlationId);

        await PublishSafelyAsync(
            () => _eventPublisher.PublishFilmUpdatedAsync(film, correlationId, cancellationToken),
            film.Id, "FilmUpdated", correlationId);

        return FilmResponse.FromEntity(film);
    }

    public async Task DeleteAsync(string id, string correlationId, CancellationToken cancellationToken = default)
    {
        var film = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.ForEntity("Filme", id);

        var deletedAt = DateTime.UtcNow;
        await _repository.SoftDeleteAsync(id, deletedAt, cancellationToken);
        await InvalidateFilmCacheAsync(id, cancellationToken);

        film.IsDeleted = true;
        film.Active = false;
        film.UpdatedAt = deletedAt;

        _logger.LogInformation("Film {FilmId} soft-deleted. CorrelationId={CorrelationId}", id, correlationId);

        await PublishSafelyAsync(
            () => _eventPublisher.PublishFilmDeletedAsync(film, correlationId, cancellationToken),
            film.Id, "FilmDeleted", correlationId);
    }

    private Task InvalidateFilmCacheAsync(string id, CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            _cache.RemoveAsync(BuildDetailCacheKey(id), cancellationToken),
            _cache.RemoveByPrefixAsync(FilmListCacheKeyPrefix, cancellationToken));
    }

    private static string BuildDetailCacheKey(string id) => $"{FilmCacheKeyPrefix}{id}";

    /// <summary>
    /// Notification publishing is best-effort: a broken queue must not roll back a
    /// successful catalog write. Failures are logged as errors (with the correlation id)
    /// for tracing -- documented trade-off in the README.
    /// </summary>
    private async Task PublishSafelyAsync(Func<Task> publish, string filmId, string eventType, string correlationId)
    {
        try
        {
            await publish();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish {EventType} event for film {FilmId}. CorrelationId={CorrelationId}",
                eventType, filmId, correlationId);
        }
    }
}
