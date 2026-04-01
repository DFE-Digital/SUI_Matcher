namespace SUI.StorageProcessFunction.Application;

public sealed class InvalidStorageQueueMessageException : Exception
{
    public InvalidStorageQueueMessageException(string message)
        : base(message) { }

    public InvalidStorageQueueMessageException(string message, Exception innerException)
        : base(message, innerException) { }
}
