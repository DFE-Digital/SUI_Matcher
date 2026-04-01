namespace SUI.StorageProcessFunction;

public sealed class StorageProcessFunctionOptions
{
    public const string SectionName = "StorageProcessFunction";

    public string QueueName { get; set; } = "storage-process-job";

    public string ProcessedContainerName { get; init; } = "processed";
}
