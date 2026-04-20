using System.Diagnostics.CodeAnalysis;

namespace SUI.Client.StorageProcessJob;

[ExcludeFromCodeCoverage(Justification = "Still developing the use of this class")]
public sealed class StorageProcessJobOptions
{
    public const string SectionName = "StorageProcessJob";

    public string QueueName { get; init; } = "storage-process-job";

    public string ProcessedContainerName { get; init; } = "processed";
    public int MaxDequeueCount { get; init; } = 1;
    public int MessageVisibilityTimeoutMinutes { get; init; } = 10;
    public int MessageVisibilityRenewalIntervalMinutes { get; init; } = 5;
    public string? MatchApiBaseAddress { get; init; }
    public required string CsvParserName { get; init; }
}
