namespace NotificationConsumer.Application.Events;

/// <summary>
/// Mirrors MovieCatalog.Application.Events.FilmEventMessage. Deliberately duplicated
/// rather than shared via a common library: each microservice owns its own copy of the
/// integration contract, which is the usual trade-off accepted at this service-boundary
/// granularity (documented in the README).
/// </summary>
public class FilmEventMessage
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
