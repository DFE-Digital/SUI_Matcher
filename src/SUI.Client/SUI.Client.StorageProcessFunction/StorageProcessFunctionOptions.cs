namespace SUI.StorageProcessFunction;

public sealed class StorageProcessFunctionOptions
{
    public const string SectionName = "StorageProcessFunction";

    public string QueueName { get; init; } = "storage-process-job";

    public string ProcessedContainerName { get; init; } = "processed";
    public string? MatchApiBaseAddress { get; init; }
    public required string CsvParserName { get; init; }
}
