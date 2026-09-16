using System.Text.Json;
using Microsoft.Extensions.Logging;
using MovieCatalog.Application.Abstractions;
using StackExchange.Redis;

namespace MovieCatalog.Infrastructure.Caching;

/// <summary>
/// Cache-aside implementation over Redis. Every method swallows connectivity failures
/// (timeouts, connection down) and logs a warning instead of throwing, so the API keeps
/// working directly against MongoDB when Redis is unavailable -- "fallback gracioso"
/// requirement.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Redis unavailable while reading key {CacheKey}; falling back to source of truth.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            var payload = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, payload, ttl);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Redis unavailable while writing key {CacheKey}; skipping cache population.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Redis unavailable while removing key {CacheKey}.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            foreach (var endpoint in _multiplexer.GetEndPoints())
            {
                var server = _multiplexer.GetServer(endpoint);
                if (!server.IsConnected)
                {
                    continue;
                }

                foreach (var key in server.Keys(pattern: $"{prefix}*"))
                {
                    await db.KeyDeleteAsync(key);
                }
            }
        }
        catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException or ObjectDisposedException)
        {
            _logger.LogWarning(ex, "Redis unavailable while removing keys by prefix {Prefix}.", prefix);
        }
    }
}
