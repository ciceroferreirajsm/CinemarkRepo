using MovieCatalog.Domain.Enums;

namespace MovieCatalog.Application.Films.Dtos;

public class FilmListQuery
{
    public Genre? Genre { get; set; }
    public bool? Active { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Deterministic string used to build the cache key for this filter combination.</summary>
    public string ToCacheKeyFragment()
        => $"genre={Genre?.ToString() ?? "any"}&active={Active?.ToString() ?? "any"}&page={Page}&pageSize={PageSize}";
}
