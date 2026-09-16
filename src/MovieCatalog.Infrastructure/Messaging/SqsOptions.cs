namespace MovieCatalog.Infrastructure.Messaging;

/// <summary>Bound from the "Sqs" configuration section (points at LocalStack in local/dev/docker).</summary>
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
}
