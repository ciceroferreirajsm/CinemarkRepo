namespace NotificationConsumer.Infrastructure.Sqs;

/// <summary>Bound from the "Sqs" configuration section (points at LocalStack).</summary>
public class SqsOptions
{
    public const string SectionName = "Sqs";

    public string ServiceUrl { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = "test";
    public string SecretKey { get; set; } = "test";

    public string FilmCreatedQueueUrl { get; set; } = string.Empty;
    public string FilmUpdatedQueueUrl { get; set; } = string.Empty;
    public string FilmDeletedQueueUrl { get; set; } = string.Empty;

    public int PollingWaitTimeSeconds { get; set; } = 10;
    public int MaxNumberOfMessages { get; set; } = 10;

    public IEnumerable<(string Name, string Url)> Queues()
    {
        if (!string.IsNullOrWhiteSpace(FilmCreatedQueueUrl)) yield return ("film-created", FilmCreatedQueueUrl);
        if (!string.IsNullOrWhiteSpace(FilmUpdatedQueueUrl)) yield return ("film-updated", FilmUpdatedQueueUrl);
        if (!string.IsNullOrWhiteSpace(FilmDeletedQueueUrl)) yield return ("film-deleted", FilmDeletedQueueUrl);
    }
}
