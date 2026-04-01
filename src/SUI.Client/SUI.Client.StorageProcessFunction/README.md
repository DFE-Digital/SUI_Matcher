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

It will:

- create a test CSV file
- create the blob container if needed
- create the queue if needed
- upload the CSV to Azurite blob storage
- add the queue message for the function

Run it with:

```powershell
pwsh ./scripts/test-storage-process-function.ps1
```

Requirements:

- Azurite running
- Azure CLI (`az`) installed
- the function app running locally

## Queue Message Contract

The queue message body must be of EventGrid schema - See https://learn.microsoft.com/en-us/azure/event-grid/event-schema

## Example Test CSV File

The script creates a file called `test-file.csv` with this content:

```csv
Given,Family,BirthDate,Gender,AddressPostalCode
Jane,Doe,2012-05-10,Female,SW1A1AA
```

Upload it to this blob path:

```text
incoming/test-file.csv
```

For Azurite, that means:

- container: `incoming`
- blob name: `test-file.csv`

## Example Queue Message

The script adds this message body to the queue:

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "subject": "/blobServices/default/containers/incoming/blobs/test-file.csv",
    "eventType": "Microsoft.Storage.BlobCreated",
    "eventTime": "2026-04-01T12:00:00Z",
    "data": {
      "url": "https://<storage-account>.blob.core.windows.net/incoming/test-file.csv"
    },
    "dataVersion": "1",
    "metadataVersion": "1"
  }
]
```

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
