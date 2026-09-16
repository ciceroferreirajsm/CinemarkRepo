using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace NotificationConsumer.Infrastructure.Sqs;

/// <summary>Reports Healthy/Unhealthy based on whether the configured queues can be reached.</summary>
public class SqsHealthCheck : IHealthCheck
{
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsOptions _options;

    public SqsHealthCheck(IAmazonSQS sqsClient, IOptions<SqsOptions> options)
    {
        _sqsClient = sqsClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var queues = _options.Queues().ToList();
        if (queues.Count == 0)
        {
            return HealthCheckResult.Unhealthy("Nenhuma fila SQS configurada.");
        }

        try
        {
            foreach (var queue in queues)
            {
                await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = queue.Url,
                    AttributeNames = new List<string> { "QueueArn" }
                }, cancellationToken);
            }

            return HealthCheckResult.Healthy("Conexão com o SQS está saudável.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Não foi possível conectar ao SQS.", ex);
        }
    }
}
