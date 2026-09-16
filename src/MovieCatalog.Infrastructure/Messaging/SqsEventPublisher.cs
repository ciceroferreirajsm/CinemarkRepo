using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieCatalog.Application.Abstractions;
using MovieCatalog.Application.Events;
using MovieCatalog.Domain.Entities;

namespace MovieCatalog.Infrastructure.Messaging;

public class SqsEventPublisher : IEventPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsOptions _options;
    private readonly ILogger<SqsEventPublisher> _logger;

    public SqsEventPublisher(IAmazonSQS sqsClient, IOptions<SqsOptions> options, ILogger<SqsEventPublisher> logger)
    {
        _sqsClient = sqsClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishFilmCreatedAsync(Film film, string correlationId, CancellationToken cancellationToken = default)
        => PublishAsync(_options.FilmCreatedQueueUrl, film, FilmEventTypes.Created, correlationId, cancellationToken);

    public Task PublishFilmUpdatedAsync(Film film, string correlationId, CancellationToken cancellationToken = default)
        => PublishAsync(_options.FilmUpdatedQueueUrl, film, FilmEventTypes.Updated, correlationId, cancellationToken);

    public Task PublishFilmDeletedAsync(Film film, string correlationId, CancellationToken cancellationToken = default)
        => PublishAsync(_options.FilmDeletedQueueUrl, film, FilmEventTypes.Deleted, correlationId, cancellationToken);

    private async Task PublishAsync(string queueUrl, Film film, string eventType, string correlationId, CancellationToken cancellationToken)
    {
        var message = new FilmEventMessage
        {
            Id = film.Id,
            Title = film.Title,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message)
        };

        await _sqsClient.SendMessageAsync(request, cancellationToken);

        _logger.LogInformation(
            "Published {EventType} event for film {FilmId} to {QueueUrl}. CorrelationId={CorrelationId}",
            eventType, film.Id, queueUrl, correlationId);
    }
}
