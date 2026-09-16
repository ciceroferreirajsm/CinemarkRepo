using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MovieCatalog.Domain.Entities;
using MovieCatalog.Domain.Repositories;

namespace MovieCatalog.Infrastructure.Persistence;

/// <summary>
/// Generic MongoDB repository. <see cref="DefaultFilter"/> is a hook subclasses can
/// override to apply cross-cutting query rules (e.g. soft-delete exclusion) to every
/// read performed through this base class.
/// </summary>
public class MongoRepository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly IMongoCollection<T> Collection;

    public MongoRepository(MongoDbContext context, string collectionName)
    {
        Collection = context.Database.GetCollection<T>(collectionName);
    }

    protected virtual FilterDefinition<T> DefaultFilter() => Builders<T>.Filter.Empty;

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var filter = Builders<T>.Filter.And(Builders<T>.Filter.Eq(x => x.Id, id), DefaultFilter());
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(Builders<T>.Filter.Where(predicate), DefaultFilter());
        return await Collection.Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(Builders<T>.Filter.Where(predicate), DefaultFilter());
        return await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(Builders<T>.Filter.Where(predicate), DefaultFilter());
        return await Collection.Find(filter).AnyAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public virtual async Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }
}
