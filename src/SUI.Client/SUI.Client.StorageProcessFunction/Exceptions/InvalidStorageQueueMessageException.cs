using System.Diagnostics.CodeAnalysis;

namespace SUI.StorageProcessFunction.Exceptions;

[ExcludeFromCodeCoverage(Justification = "Simple exceptions")]
public sealed class InvalidStorageQueueMessageException : Exception
{
    public InvalidStorageQueueMessageException(string message)
        : base(message) { }

    public InvalidStorageQueueMessageException(string message, Exception innerException)
        : base(message, innerException) { }
}
