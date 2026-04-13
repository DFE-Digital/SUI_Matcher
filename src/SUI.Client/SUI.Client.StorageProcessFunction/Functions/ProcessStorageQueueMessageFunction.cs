using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.StorageProcessFunction.Application.Interfaces;
using SUI.StorageProcessFunction.Exceptions;

namespace SUI.StorageProcessFunction.Functions;

public sealed class ProcessStorageQueueMessageFunction(
    IStorageQueueMessageParser queueMessageParser,
    IBlobFileOrchestrator blobFileOrchestrator,
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
            await blobFileOrchestrator.ProcessAsync(
                queueMessageParser.Parse(queueMessage),
                cancellationToken
            );
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
