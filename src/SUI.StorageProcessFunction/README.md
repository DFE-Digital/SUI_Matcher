# SUI.StorageProcessFunction

## Manual Test

This function currently:

- reads one Azure Storage Queue message
- expects a typed message body with `containerName` and `blobName`
- downloads the referenced blob
- passes the blob content to a placeholder processor
- throws `NotSupportedException` because real file processing is not implemented yet

Azure Functions handles:

- queue trigger activation
- invisibility/lease handling during execution
- retry behavior
- poison queue behavior based on `host.json`

## Queue Message Contract

The queue message body must be JSON with this shape:

```json
{
  "containerName": "incoming",
  "blobName": "test-file.csv"
}
```

## Example Test CSV File

Create a file called `test-file.csv` with this content:

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

Add this message body to the queue:

```json
{
  "containerName": "incoming",
  "blobName": "test-file.csv"
}
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
- the placeholder processor should throw `NotSupportedException`
- Azure Functions should retry according to the queue trigger settings in `host.json`
