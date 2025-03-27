using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Serilog;

Console.WriteLine("SUI E2E Blob Container File Sync");
if (args.Length == 0)
{
    Console.WriteLine("args[0] = container name; args[1] = local base folder path to sync");
    return;
}

const string EnvVarNameBlobStorageConnecitionString = "SUI_E2E_BLOB_STORAGE_CONNECTION_STRING";
var blobStorageConnectionString = Environment.GetEnvironmentVariable(EnvVarNameBlobStorageConnecitionString, EnvironmentVariableTarget.Machine) ?? throw new Exception($"Env var '{EnvVarNameBlobStorageConnecitionString}' value not set");
var containerName = args.ElementAtOrDefault(0) ?? throw new Exception("arg[0]: container-name not set");
var localFolderPath = args.ElementAtOrDefault(1) ?? throw new Exception("arg[1]: local-folder-path not set");
var containerClient = new BlobContainerClient(blobStorageConnectionString, containerName);

var syncInterval = 5000; // Sync every 5 seconds
var running = true;
var maxRetries = 10;
var retryDelay = 2000; // 2 seconds

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Output to console
    .WriteTo.File("./Logs/log-.txt", rollingInterval: RollingInterval.Day) // Roll logs daily
    .CreateLogger();

// Create logger using Serilog
var logger = new LoggerFactory()
    .AddSerilog()
    .CreateLogger<Program>();

logger.LogInformation($"Synchronising local folder '{localFolderPath}' with container '{containerName}' on account {containerClient.AccountName}");

logger.LogInformation("Starting synchronization...");
logger.LogInformation("Press 'c' to stop.");

_ = Task.Run(() =>
{
    while (Console.ReadKey(true).Key != ConsoleKey.C) { }
    running = false;
});

while (running)
{
    await ExecuteWithRetries(SyncToAzureAsync);
    await ExecuteWithRetries(SyncToLocalAsync);
    logger.LogInformation("Synchronization completed. Waiting...");
    await Task.Delay(syncInterval);
}

logger.LogInformation("Synchronization stopped.");



async Task ExecuteWithRetries(Func<Task> action)
{
    var attempt = 0;
    while (attempt < maxRetries)
    {
        try
        {
            await action();
            return;
        }
        catch (Exception ex)
        {
            attempt++;
            logger.LogWarning($"Attempt {attempt}/{maxRetries} failed: {ex.Message}");
            if (attempt == maxRetries)
            {
                logger.LogError("Maximum retries reached. Operation failed.");
                return;
            }
            await Task.Delay(retryDelay);
        }
    }
}

async Task SyncToAzureAsync()
{
    await containerClient.CreateIfNotExistsAsync();

    var localFiles = Directory.GetFiles(localFolderPath, "*", SearchOption.AllDirectories);
    foreach (var file in localFiles)
    {
        var relativePath = Path.GetRelativePath(localFolderPath, file).Replace("\\", "/");
        var blobClient = containerClient.GetBlobClient(relativePath);

        if (!await blobClient.ExistsAsync())
        {
            logger.LogInformation($"Uploading {relativePath} to Azure...");
            await blobClient.UploadAsync(file, true);
        }
    }
}

async Task SyncToLocalAsync()
{
    await containerClient.CreateIfNotExistsAsync();
    await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
    {
        var localFilePath = Path.Combine(localFolderPath, blobItem.Name.Replace("/", "\\"));
        var localDirectory = Path.GetDirectoryName(localFilePath);
        if (!Directory.Exists(localDirectory))
        {
            Directory.CreateDirectory(localDirectory);
        }
        if (!File.Exists(localFilePath))
        {
            logger.LogInformation($"Downloading {blobItem.Name} from Azure...");
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            await blobClient.DownloadToAsync(localFilePath);
        }
    }
}
