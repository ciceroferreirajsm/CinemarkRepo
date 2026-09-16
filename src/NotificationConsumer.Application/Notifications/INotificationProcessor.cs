using NotificationConsumer.Application.Events;

namespace NotificationConsumer.Application.Notifications;

/// <summary>Handles a single Film integration event received from SQS.</summary>
public interface INotificationProcessor
{
    Task ProcessAsync(string queueName, FilmEventMessage message, CancellationToken cancellationToken = default);
}
