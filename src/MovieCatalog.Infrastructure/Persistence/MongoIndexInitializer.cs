using MongoDB.Driver;
using MovieCatalog.Domain.Entities;

namespace MovieCatalog.Infrastructure.Persistence;

/// <summary>
/// Creates the indexes the Films collection needs: single-field indexes for the most
/// common filters (Genre, Active, IsDeleted) plus a composite index that covers the
/// paginated listing query (IsDeleted + Active + Genre, ordered by CreatedAt).
/// Index creation is idempotent, so this can run on every startup.
/// </summary>
public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(MongoDbContext context, CancellationToken cancellationToken = default)
    {
        var collection = context.Database.GetCollection<Film>(context.Options.FilmsCollectionName);

        var indexModels = new[]
        {
            new CreateIndexModel<Film>(Builders<Film>.IndexKeys.Ascending(f => f.Genre)),
            new CreateIndexModel<Film>(Builders<Film>.IndexKeys.Ascending(f => f.Active)),
            new CreateIndexModel<Film>(Builders<Film>.IndexKeys.Ascending(f => f.IsDeleted)),
            new CreateIndexModel<Film>(
                Builders<Film>.IndexKeys
                    .Ascending(f => f.IsDeleted)
                    .Ascending(f => f.Active)
                    .Ascending(f => f.Genre)
                    .Descending(f => f.CreatedAt)),
            new CreateIndexModel<Film>(
                Builders<Film>.IndexKeys.Ascending(f => f.Title),
                new CreateIndexOptions { Name = "ix_title_lookup" })
        };

        await collection.Indexes.CreateManyAsync(indexModels, cancellationToken);
    }
}
