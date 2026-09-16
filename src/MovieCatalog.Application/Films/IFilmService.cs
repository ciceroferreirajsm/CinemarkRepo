using MovieCatalog.Application.Common;
using MovieCatalog.Application.Films.Dtos;

namespace MovieCatalog.Application.Films;

public interface IFilmService
{
    Task<FilmResponse> CreateAsync(CreateFilmRequest request, string correlationId, CancellationToken cancellationToken = default);

    Task<FilmResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResult<FilmResponse>> GetListAsync(FilmListQuery query, CancellationToken cancellationToken = default);

    Task<FilmResponse> UpdateAsync(string id, UpdateFilmRequest request, string correlationId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, string correlationId, CancellationToken cancellationToken = default);
}
