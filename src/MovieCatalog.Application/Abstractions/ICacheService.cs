namespace MovieCatalog.Application.Abstractions;

/// <summary>
/// Cache-aside abstraction over Redis. Implementations MUST fail gracefully:
/// if the cache backend is unavailable, they log a warning and behave as a
/// no-op/miss rather than throwing, so the API keeps serving from MongoDB.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes every key that starts with <paramref name="prefix"/> (used to invalidate list caches).</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
