namespace MovieCatalog.Infrastructure.Persistence;

/// <summary>Bound from the "MongoDb" configuration section.</summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string FilmsCollectionName { get; set; } = "films";

    /// <summary>Connection pool sizing -- see requirement "connection pooling adequado".</summary>
    public int MaxConnectionPoolSize { get; set; } = 100;
    public int MinConnectionPoolSize { get; set; } = 5;
    public int ConnectTimeoutSeconds { get; set; } = 10;
    public int ServerSelectionTimeoutSeconds { get; set; } = 10;
}
