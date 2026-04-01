using Shared;

namespace SUI.StorageProcessFunction;

public sealed class StorageProcessFunctionOptions
{
    public const string SectionName = "StorageProcessFunction";

    public string QueueName { get; set; } = "storage-process-job";

    public string ProcessedContainerName { get; init; } = "processed";
    public string? MatchApiBaseAddress { get; set; }
    public string SearchStrategy { get; set; } =
        SharedConstants.SearchStrategy.Strategies.Strategy4;
    public int? StrategyVersion { get; set; } = 2;
    public int MaxRequestsPerSecond { get; set; } = 10;
}
