using MovieCatalog.Domain.Entities;

namespace MovieCatalog.Domain.Repositories;

/// <summary>
/// Film-specific repository. Extends the generic <see cref="IRepository{T}"/> with
/// soft-delete semantics and the title-uniqueness check, both of which are Film
/// business rules and therefore do not belong in the generic contract.
/// </summary>
public interface IFilmRepository : IRepository<Film>
{
    /// <summary>Marks the film as deleted (IsDeleted = true, Active = false) instead of removing the document.</summary>
    Task SoftDeleteAsync(string id, DateTime deletedAt, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive title lookup among non-deleted films, optionally excluding one id (used on update).</summary>
    Task<bool> ExistsByTitleAsync(string title, string? excludeId = null, CancellationToken cancellationToken = default);
}
