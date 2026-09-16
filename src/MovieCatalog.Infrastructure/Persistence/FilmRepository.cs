using MongoDB.Driver;
using MovieCatalog.Domain.Entities;
using MovieCatalog.Domain.Repositories;

namespace MovieCatalog.Infrastructure.Persistence;

/// <summary>
/// Film repository. Overrides <see cref="DefaultFilter"/> so every read from the base
/// class (GetById/Find/Count/Exists) automatically excludes soft-deleted documents,
/// satisfying "queries padrão devem filtrar automaticamente" without repeating the
/// IsDeleted check on every call site.
/// </summary>
public class FilmRepository : MongoRepository<Film>, IFilmRepository
{
    public FilmRepository(MongoDbContext context) : base(context, context.Options.FilmsCollectionName)
    {
    }

    protected override FilterDefinition<Film> DefaultFilter()
        => Builders<Film>.Filter.Eq(f => f.IsDeleted, false);

    public async Task SoftDeleteAsync(string id, DateTime deletedAt, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Film>.Filter.Eq(f => f.Id, id);
        var update = Builders<Film>.Update
            .Set(f => f.IsDeleted, true)
            .Set(f => f.Active, false)
            .Set(f => f.UpdatedAt, deletedAt);

        await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsByTitleAsync(string title, string? excludeId = null, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Film>.Filter.And(
            Builders<Film>.Filter.Eq(f => f.IsDeleted, false),
            Builders<Film>.Filter.Regex(f => f.Title, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(title)}$", "i")));

        if (!string.IsNullOrEmpty(excludeId))
        {
            filter = Builders<Film>.Filter.And(filter, Builders<Film>.Filter.Ne(f => f.Id, excludeId));
        }

        return await Collection.Find(filter).AnyAsync(cancellationToken);
    }
}
