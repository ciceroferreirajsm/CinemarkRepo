using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationConsumer.Application.Events;
using NotificationConsumer.Application.Notifications;

namespace NotificationConsumer.Infrastructure.Sqs;

/// <summary>
/// Long-polls the FilmCreated/FilmUpdated/FilmDeleted queues concurrently and hands
/// each message to <see cref="INotificationProcessor"/>. Runs independently of the
/// Movie Catalog API process/container.
/// </summary>
public class SqsConsumerBackgroundService : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly INotificationProcessor _processor;
    private readonly SqsOptions _options;
    private readonly ILogger<SqsConsumerBackgroundService> _logger;

    public SqsConsumerBackgroundService(
        IAmazonSQS sqsClient,
        INotificationProcessor processor,
        IOptions<SqsOptions> options,
        ILogger<SqsConsumerBackgroundService> logger)
    {
        _sqsClient = sqsClient;
        _processor = processor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queues = _options.Queues().ToList();
        if (queues.Count == 0)
        {
            _logger.LogWarning("No SQS queue URLs configured; the consumer has nothing to poll.");
            return;
        }

        var pollingTasks = queues.Select(queue => PollQueueAsync(queue.Name, queue.Url, stoppingToken));
        await Task.WhenAll(pollingTasks);
    }

    private async Task PollQueueAsync(string queueName, string queueUrl, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = _options.MaxNumberOfMessages,
                    WaitTimeSeconds = _options.PollingWaitTimeSeconds
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await HandleMessageAsync(queueName, queueUrl, message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling queue {QueueName}. Retrying shortly.", queueName);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown.
                }
            }
        }
    }

    private async Task HandleMessageAsync(string queueName, string queueUrl, Message message, CancellationToken cancellationToken)
    {
        try
        {
            var eventMessage = JsonSerializer.Deserialize<FilmEventMessage>(message.Body);
            if (eventMessage is null)
            {
                _logger.LogWarning("Could not deserialize message {MessageId} from queue {QueueName}.", message.MessageId, queueName);
                return;
            }

            await _processor.ProcessAsync(queueName, eventMessage, cancellationToken);

            await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = message.ReceiptHandle
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId} from queue {QueueName}; leaving it for retry.", message.MessageId, queueName);
        }
    }
}
