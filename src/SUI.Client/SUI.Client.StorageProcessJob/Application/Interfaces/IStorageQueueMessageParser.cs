namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IStorageQueueMessageParser
{
    StorageBlobMessage Parse(string queueMessage);
}
