namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IBlobFileOrchestrator
{
    Task ProcessAsync(StorageBlobMessage queueMessage, CancellationToken cancellationToken);
}
