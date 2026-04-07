using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.StorageProcessFunction.Application;

namespace SUI.StorageProcessFunction.Functions;

public sealed class ProcessStorageQueueMessageFunction(
    IStorageQueueMessageParser queueMessageParser,
    IStorageQueueMessageProcessor processor,
    ILogger<ProcessStorageQueueMessageFunction> logger
)
{
    [Function(nameof(ProcessStorageQueueMessageFunction))]
    public async Task RunAsync(
        [QueueTrigger("%QueueName%", Connection = "AzureWebJobsStorage")] string queueMessage,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Processing storage queue message.");

        try
        {
            await processor.ProcessAsync(queueMessageParser.Parse(queueMessage), cancellationToken);
        }
        catch (InvalidStorageQueueMessageException ex)
        {
            logger.LogError(
                ex,
                "Storage queue message failed validation. This may indicate Event Grid subscription misconfiguration. Queue message: {QueueMessage}",
                queueMessage
            );
            throw;
        }
    }
}
