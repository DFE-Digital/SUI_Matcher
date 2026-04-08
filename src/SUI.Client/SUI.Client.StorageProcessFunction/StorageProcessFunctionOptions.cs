using Shared;

namespace SUI.StorageProcessFunction;

public sealed class StorageProcessFunctionOptions
{
    public const string SectionName = "StorageProcessFunction";

    public string QueueName { get; init; } = "storage-process-job";

    public string ProcessedContainerName { get; init; } = "processed";
    public string? MatchApiBaseAddress { get; init; }
    public string SearchStrategy { get; init; } =
        SharedConstants.SearchStrategy.Strategies.Strategy4;
    public int? StrategyVersion { get; init; } = 2;
    public required string CsvParserName { get; init; }

    public static class CsvParserNameConstants
    {
        public const string TypeOne = "TypeOne";
    }
}
