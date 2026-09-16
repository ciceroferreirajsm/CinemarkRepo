namespace MovieCatalog.Application.Common;

/// <summary>Bound from the "Cache" configuration section. TTLs are configurable per requirement.</summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    public int FilmDetailTtlMinutes { get; set; } = 5;
    public int FilmListTtlMinutes { get; set; } = 5;
}
