namespace SUI.Client.StorageProcessJob.Application;

public record StorageBlobMessage(string? ContainerName, string? BlobName)
{
    public (bool IsValid, string? ValidationMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(ContainerName))
        {
            return (false, $"Queue message did not contain {ContainerName}.");
        }

        if (string.IsNullOrWhiteSpace(BlobName))
        {
            return (false, $"Queue message did not contain {BlobName}.");
        }

        return (true, null);
    }
}
