namespace SUI.StorageProcessFunction;

public sealed class StorageProcessFunctionOptions
{
    public const string SectionName = "StorageProcessFunction";

    public string StorageConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "storage-process-job";
}
