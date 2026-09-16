namespace MovieCatalog.Application.Events;

/// <summary>
/// Integration event payload published to SQS for FilmCreated / FilmUpdated / FilmDeleted.
/// Intentionally duplicated (not shared as a library) in NotificationConsumer.Application,
/// since each microservice owns its own contract copy -- documented in the README.
/// </summary>
public class FilmEventMessage
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public static class FilmEventTypes
{
    public const string Created = "FilmCreated";
    public const string Updated = "FilmUpdated";
    public const string Deleted = "FilmDeleted";
}
