namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IStorageQueueMessageParser
{
    StorageBlobMessage Parse(string queueMessage);
}
