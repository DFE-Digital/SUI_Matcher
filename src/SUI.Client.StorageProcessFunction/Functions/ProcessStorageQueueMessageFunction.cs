using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.StorageProcessFunction.Application;

namespace SUI.StorageProcessFunction.Functions;

public sealed class ProcessStorageQueueMessageFunction(
    StorageQueueMessageProcessor processor,
    ILogger<ProcessStorageQueueMessageFunction> logger
)
{
    [Function(nameof(ProcessStorageQueueMessageFunction))]
    public async Task RunAsync(
        [QueueTrigger("%QueueName%", Connection = "AzureWebJobsStorage")]
            StorageBlobMessage queueMessage,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Processing storage queue message.");
        await processor.ProcessAsync(queueMessage, cancellationToken);
    }
}
