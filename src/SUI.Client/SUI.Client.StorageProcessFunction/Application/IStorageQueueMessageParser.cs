namespace SUI.StorageProcessFunction.Application;

public interface IStorageQueueMessageParser
{
    StorageBlobMessage Parse(string queueMessage);
}
