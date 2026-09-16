using MovieCatalog.Domain.Entities;

namespace MovieCatalog.Application.Abstractions;

/// <summary>Publishes Film domain events to the messaging backend (SQS/LocalStack).</summary>
public interface IEventPublisher
{
    Task PublishFilmCreatedAsync(Film film, string correlationId, CancellationToken cancellationToken = default);

    Task PublishFilmUpdatedAsync(Film film, string correlationId, CancellationToken cancellationToken = default);

    Task PublishFilmDeletedAsync(Film film, string correlationId, CancellationToken cancellationToken = default);
}
