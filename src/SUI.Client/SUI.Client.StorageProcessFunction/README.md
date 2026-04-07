# SUI.StorageProcessFunction

## Manual Test

This function currently:

- reads one Azure Storage Queue message
- expects an Azure Event Grid schema message for a blob-created event
- only processes files from the `incoming` container
- downloads the referenced blob
- passes the blob content to a placeholder processor
- treats the placeholder processing step as successful
- moves the blob into the `processed` container using the path format `ddMMyyHHmmss_filename/filename`

Azure Functions handles:

- queue trigger activation
- invisibility/lease handling during execution
- retry behavior
- poison queue behavior based on `host.json`

## Test Script

Use the cross-platform PowerShell script at `scripts/test-storage-process-function.ps1`.
Prerequisites:

- Azure CLI (`az`) installed
- the target storage account or Azurite running and reachable
- the storage-process function running locally if you want to observe end-to-end processing

The script includes PowerShell comment-based help, so use `Get-Help ./scripts/test-storage-process-function.ps1 -Detailed`
or `Get-Help ./scripts/test-storage-process-function.ps1 -Examples` for usage and examples.

Note: for Mac users, enter powershell mode with `pwsh` first before running the Get-Help command.

## Queue Message Contract

The queue message body must be of EventGrid schema - See https://learn.microsoft.com/en-us/azure/event-grid/event-schema

## Local Settings

Your `local.settings.json` should contain at least:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "QueueName": "storage-process-job"
  }
}
```

## Expected Result

For the current scaffold:

- the function should trigger from the queue message
- the blob should be downloaded successfully
- the placeholder processor should complete successfully
- the original blob should be removed from `incoming`
- the file should be written to the `processed` container with a timestamped folder name
