namespace SUI.StorageProcessFunction.Application;

public interface IStorageQueueMessageProcessor
{
    Task ProcessAsync(StorageBlobMessage queueMessage, CancellationToken cancellationToken);
}
