namespace SUI.StorageProcessFunction.Application;

public sealed class StorageBlobMessage
{
    public string? ContainerName { get; set; }

    public string? BlobName { get; set; }
}
