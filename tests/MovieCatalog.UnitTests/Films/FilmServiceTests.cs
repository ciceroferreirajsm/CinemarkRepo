using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MovieCatalog.Application.Abstractions;
using MovieCatalog.Application.Common;
using MovieCatalog.Application.Films;
using MovieCatalog.Application.Films.Dtos;
using MovieCatalog.Domain.Entities;
using MovieCatalog.Domain.Enums;
using MovieCatalog.Domain.Exceptions;
using MovieCatalog.Domain.Repositories;
using Xunit;

namespace MovieCatalog.UnitTests.Films;

public class FilmServiceTests
{
    private readonly Mock<IFilmRepository> _repository = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly FilmService _sut;

    public FilmServiceTests()
    {
        var cacheOptions = Options.Create(new CacheOptions { FilmDetailTtlMinutes = 5, FilmListTtlMinutes = 5 });
        _sut = new FilmService(_repository.Object, _cache.Object, _eventPublisher.Object, cacheOptions, NullLogger<FilmService>.Instance);
    }

    private static Film SampleFilm(string id = "662f1b7e2c1a4e0012a3f9d1", string title = "Duna") => new()
    {
        Id = id,
        Title = title,
        Genre = Genre.FiccaoCientifica,
        ReleaseDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        DurationMinutes = 166,
        Rating = 8.7m,
        Active = true,
        IsDeleted = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_WhenTitleIsDuplicate_ThrowsBusinessRuleValidationException()
    {
        _repository.Setup(r => r.ExistsByTitleAsync("Duna", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new CreateFilmRequest { Title = "Duna", Genre = Genre.FiccaoCientifica, ReleaseDate = DateTime.UtcNow, DurationMinutes = 100, Rating = 5 };

        var act = () => _sut.CreateAsync(request, "corr-1");

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Film>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTitleIsUnique_CreatesFilm_InvalidatesListCache_AndPublishesEvent()
    {
        _repository.Setup(r => r.ExistsByTitleAsync("Duna", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = new CreateFilmRequest { Title = "Duna", Genre = Genre.FiccaoCientifica, ReleaseDate = DateTime.UtcNow, DurationMinutes = 166, Rating = 8.7m };

        var response = await _sut.CreateAsync(request, "corr-1");

        response.Title.Should().Be("Duna");
        _repository.Verify(r => r.AddAsync(It.Is<Film>(f => f.Title == "Duna" && !f.IsDeleted), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPrefixAsync("films:list:", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishFilmCreatedAsync(It.IsAny<Film>(), "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEventPublishingFails_DoesNotThrowAndStillReturnsResponse()
    {
        _repository.Setup(r => r.ExistsByTitleAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _eventPublisher.Setup(p => p.PublishFilmCreatedAsync(It.IsAny<Film>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SQS unreachable"));
        var request = new CreateFilmRequest { Title = "Duna", Genre = Genre.FiccaoCientifica, ReleaseDate = DateTime.UtcNow, DurationMinutes = 166, Rating = 8.7m };

        var response = await _sut.CreateAsync(request, "corr-1");

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_ReturnsCachedResponseWithoutHittingRepository()
    {
        var cached = new FilmResponse { Id = "1", Title = "Cached" };
        _cache.Setup(c => c.GetAsync<FilmResponse>("films:1", It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await _sut.GetByIdAsync("1");

        result.Should().BeSameAs(cached);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMissAndFilmExists_ReturnsFromRepositoryAndPopulatesCache()
    {
        var film = SampleFilm();
        _cache.Setup(c => c.GetAsync<FilmResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((FilmResponse?)null);
        _repository.Setup(r => r.GetByIdAsync(film.Id, It.IsAny<CancellationToken>())).ReturnsAsync(film);

        var result = await _sut.GetByIdAsync(film.Id);

        result.Id.Should().Be(film.Id);
        _cache.Verify(c => c.SetAsync($"films:{film.Id}", It.IsAny<FilmResponse>(), TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFilmDoesNotExist_ThrowsNotFoundException()
    {
        _cache.Setup(c => c.GetAsync<FilmResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((FilmResponse?)null);
        _repository.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Film?)null);

        var act = () => _sut.GetByIdAsync("missing");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetListAsync_WhenCacheHit_ReturnsCachedResultWithoutQueryingRepository()
    {
        var cached = new PagedResult<FilmResponse> { Items = new List<FilmResponse>(), Page = 1, PageSize = 10, TotalCount = 0 };
        _cache.Setup(c => c.GetAsync<PagedResult<FilmResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await _sut.GetListAsync(new FilmListQuery());

        result.Should().BeSameAs(cached);
        _repository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Film, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetListAsync_WhenCacheMiss_ClampsPagingAndCachesResult()
    {
        _cache.Setup(c => c.GetAsync<PagedResult<FilmResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PagedResult<FilmResponse>?)null);
        _repository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Film, bool>>>(), 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Film> { SampleFilm() });
        _repository.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Film, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.GetListAsync(new FilmListQuery { Page = 0, PageSize = 999 });

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        result.TotalCount.Should().Be(1);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<FilmResponse>>(), TimeSpan.FromMinutes(5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenFilmDoesNotExist_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Film?)null);
        var request = new UpdateFilmRequest { Title = "X", Genre = Genre.Drama, ReleaseDate = DateTime.UtcNow, DurationMinutes = 100, Rating = 5, Active = true };

        var act = () => _sut.UpdateAsync("missing", request, "corr-1");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenTitleChangedToAnExistingOne_ThrowsBusinessRuleValidationException()
    {
        var film = SampleFilm(title: "Old Title");
        _repository.Setup(r => r.GetByIdAsync(film.Id, It.IsAny<CancellationToken>())).ReturnsAsync(film);
        _repository.Setup(r => r.ExistsByTitleAsync("New Title", film.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new UpdateFilmRequest { Title = "New Title", Genre = Genre.Drama, ReleaseDate = DateTime.UtcNow, DurationMinutes = 100, Rating = 5, Active = true };

        var act = () => _sut.UpdateAsync(film.Id, request, "corr-1");

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenTitleUnchanged_UpdatesSuccessfully_InvalidatesCache_AndPublishesEvent()
    {
        var film = SampleFilm(title: "Duna");
        _repository.Setup(r => r.GetByIdAsync(film.Id, It.IsAny<CancellationToken>())).ReturnsAsync(film);
        var request = new UpdateFilmRequest { Title = "Duna", Genre = Genre.Drama, ReleaseDate = DateTime.UtcNow, DurationMinutes = 120, Rating = 9, Active = false };

        var response = await _sut.UpdateAsync(film.Id, request, "corr-1");

        response.DurationMinutes.Should().Be(120);
        response.Active.Should().BeFalse();
        _repository.Verify(r => r.ExistsByTitleAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.UpdateAsync(film.Id, It.IsAny<Film>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"films:{film.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPrefixAsync("films:list:", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishFilmUpdatedAsync(It.IsAny<Film>(), "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenFilmDoesNotExist_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Film?)null);

        var act = () => _sut.DeleteAsync("missing", "corr-1");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenFilmExists_SoftDeletes_InvalidatesCache_AndPublishesEvent()
    {
        var film = SampleFilm();
        _repository.Setup(r => r.GetByIdAsync(film.Id, It.IsAny<CancellationToken>())).ReturnsAsync(film);

        await _sut.DeleteAsync(film.Id, "corr-1");

        _repository.Verify(r => r.SoftDeleteAsync(film.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"films:{film.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveByPrefixAsync("films:list:", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishFilmDeletedAsync(It.IsAny<Film>(), "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
