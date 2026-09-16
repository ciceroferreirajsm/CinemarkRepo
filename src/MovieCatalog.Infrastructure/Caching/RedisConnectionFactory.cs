using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MovieCatalog.Infrastructure.Caching;

/// <summary>
/// Creates the shared <see cref="IConnectionMultiplexer"/> with AbortOnConnectFail
/// disabled, so a Redis outage at startup never prevents the API from booting -- the
/// multiplexer keeps retrying in the background and RedisCacheService treats every
/// call as a graceful miss until it reconnects.
/// </summary>
public static class RedisConnectionFactory
{
    public static IConnectionMultiplexer Create(RedisOptions options, ILogger logger)
    {
        try
        {
            var configuration = ConfigurationOptions.Parse(options.ConnectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectRetry = 3;
            configuration.ConnectTimeout = 5000;
            return ConnectionMultiplexer.Connect(configuration);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not establish an initial Redis connection; the API will keep working directly against MongoDB until Redis becomes available.");
            var fallbackConfiguration = ConfigurationOptions.Parse(options.ConnectionString);
            fallbackConfiguration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(fallbackConfiguration);
        }
    }
}
