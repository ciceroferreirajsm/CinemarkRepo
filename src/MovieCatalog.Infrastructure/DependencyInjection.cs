using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieCatalog.Application.Abstractions;
using MovieCatalog.Domain.Repositories;
using MovieCatalog.Infrastructure.Caching;
using MovieCatalog.Infrastructure.Messaging;
using MovieCatalog.Infrastructure.Persistence;
using StackExchange.Redis;

namespace MovieCatalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMovieCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<SqsOptions>(configuration.GetSection(SqsOptions.SectionName));

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IFilmRepository, FilmRepository>();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            return RedisConnectionFactory.Create(options, logger);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var sqsOptions = configuration.GetSection(SqsOptions.SectionName).Get<SqsOptions>() ?? new SqsOptions();
            var config = new AmazonSQSConfig { ServiceURL = sqsOptions.ServiceUrl };
            var credentials = new BasicAWSCredentials(sqsOptions.AccessKey, sqsOptions.SecretKey);
            return new AmazonSQSClient(credentials, config);
        });
        services.AddScoped<IEventPublisher, SqsEventPublisher>();

        return services;
    }
}
