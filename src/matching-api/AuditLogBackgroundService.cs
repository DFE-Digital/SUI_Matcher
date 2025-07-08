using System.Threading.Channels;

using Azure.Data.Tables;

using Shared;
using Shared.Logging;

namespace MatchingApi;

public class AuditLogBackgroundService(
    ILogger<AuditLogBackgroundService> logger,
    Channel<AuditLogEntry> channel,
    TableServiceClient client,
    IHttpContextAccessor httpContextAccessor)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await client.CreateTableIfNotExistsAsync(Constants.AuditLog.AzStorageTableName, stoppingToken);

        await foreach (var entry in channel.Reader.ReadAllAsync(stoppingToken))
        {
            Console.WriteLine("Writing audit log entry");
            var user = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "UnknownUser";
            if (user == "UnknownUser")
            {
                logger.LogWarning("[AUDIT] Audit log entry created with unknown user.");
            }

            var timeStamp = DateTime.UtcNow;
            var partitionKey = $"{user}_{timeStamp:yyyy-MM-dd}";
            try
            {
                var tableClient = client.GetTableClient(Constants.AuditLog.AzStorageTableName);

                // ! Will always be Unknown user until we implement authentication
                var entity = new TableEntity
                {
                    PartitionKey = partitionKey,
                    RowKey = Guid.NewGuid().ToString("N"),
                    ["UserId"] = user,
                    ["Action"] = entry.Action.ToString(),
                    ["Timestamp"] = timeStamp,
                    ["SearchId"] = entry.Metadata.GetValueOrDefault("SearchId", string.Empty),
                    ["Metadata"] = JsonSerializer.Serialize(entry.Metadata)
                };
                await tableClient.AddEntityAsync(entity, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write audit log entry: {Message}", ex.Message);
            }
        }
    }
}