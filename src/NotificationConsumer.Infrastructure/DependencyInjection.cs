using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NotificationConsumer.Infrastructure.Sqs;

namespace NotificationConsumer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationConsumerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqsOptions>(configuration.GetSection(SqsOptions.SectionName));

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var sqsOptions = configuration.GetSection(SqsOptions.SectionName).Get<SqsOptions>() ?? new SqsOptions();
            var config = new AmazonSQSConfig { ServiceURL = sqsOptions.ServiceUrl };
            var credentials = new BasicAWSCredentials(sqsOptions.AccessKey, sqsOptions.SecretKey);
            return new AmazonSQSClient(credentials, config);
        });

        services.AddHostedService<SqsConsumerBackgroundService>();

        services.AddHealthChecks()
            .AddCheck<SqsHealthCheck>("sqs", tags: new[] { "ready" });

        return services;
    }
}
