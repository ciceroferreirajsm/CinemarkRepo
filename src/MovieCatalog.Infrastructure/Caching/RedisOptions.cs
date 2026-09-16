namespace MovieCatalog.Infrastructure.Caching;

/// <summary>Bound from the "Redis" configuration section.</summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
}
