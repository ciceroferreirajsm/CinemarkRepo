using System.Linq.Expressions;
using MovieCatalog.Domain.Entities;

namespace MovieCatalog.Domain.Repositories;

/// <summary>
/// Generic, technology-agnostic repository contract. Implementations live in
/// Infrastructure (MongoDB in this project) and translate the LINQ predicates
/// into the underlying query language.
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);
}
