namespace SUI.Client.StorageProcessJob;

public sealed class StorageProcessJobOptions
{
    public const string SectionName = "StorageProcessJob";

    public string QueueName { get; init; } = "storage-process-job";

    public string ProcessedContainerName { get; init; } = "processed";
    public string? MatchApiBaseAddress { get; init; }
    public required string CsvParserName { get; init; }
}
