using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MovieCatalog.Infrastructure.Persistence;

/// <summary>
/// Wraps the singleton <see cref="IMongoClient"/>/<see cref="IMongoDatabase"/> pair.
/// The driver already pools connections per client instance, so this type is
/// registered once and reused across the app -- pool sizing comes from
/// <see cref="MongoDbOptions"/>.
/// </summary>
public class MongoDbContext
{
    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }
    public MongoDbOptions Options { get; }

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        Options = options.Value;

        var settings = MongoClientSettings.FromConnectionString(Options.ConnectionString);
        settings.MaxConnectionPoolSize = Options.MaxConnectionPoolSize;
        settings.MinConnectionPoolSize = Options.MinConnectionPoolSize;
        settings.ConnectTimeout = TimeSpan.FromSeconds(Options.ConnectTimeoutSeconds);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(Options.ServerSelectionTimeoutSeconds);

        Client = new MongoClient(settings);
        Database = Client.GetDatabase(Options.DatabaseName);
    }
}
