using Microsoft.Extensions.Logging;
using NotificationConsumer.Application.Events;

namespace NotificationConsumer.Application.Notifications;

public class NotificationProcessor : INotificationProcessor
{
    private static readonly HashSet<string> KnownEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FilmCreated",
        "FilmUpdated",
        "FilmDeleted"
    };

    private readonly ILogger<NotificationProcessor> _logger;

    public NotificationProcessor(ILogger<NotificationProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(string queueName, FilmEventMessage message, CancellationToken cancellationToken = default)
    {
        if (!KnownEventTypes.Contains(message.EventType))
        {
            _logger.LogWarning(
                "Received event with unknown type {EventType} from queue {QueueName}. FilmId={FilmId} CorrelationId={CorrelationId}",
                message.EventType, queueName, message.Id, message.CorrelationId);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Notification received: EventType={EventType} FilmTitle={FilmTitle} Timestamp={Timestamp} CorrelationId={CorrelationId}",
            message.EventType, message.Title, message.Timestamp, message.CorrelationId);

        return Task.CompletedTask;
    }
}
