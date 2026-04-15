namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IBlobFileOrchestrator
{
    Task ProcessAsync(StorageBlobMessage queueMessage, CancellationToken cancellationToken);
}
