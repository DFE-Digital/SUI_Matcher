<#
.SYNOPSIS
Creates a local test file, uploads it to storage, and enqueues an Event Grid message for the storage-process function.

.DESCRIPTION
Use this script to exercise the storage-process function locally against Azurite or another Azure Storage account.
The script creates a CSV file, ensures the target blob container and queue exist, uploads the blob, and posts a single
Event Grid schema message with eventType `Microsoft.Storage.BlobCreated`.

.PARAMETER ConnectionString
The Azure Storage connection string used for blob and queue operations. Defaults to `UseDevelopmentStorage=true`.

.PARAMETER ContainerName
The source blob container to upload into. The storage-process function currently only accepts messages for `incoming`.

.PARAMETER QueueName
The Azure Storage queue name that the storage-process function listens to. Defaults to `storage-process-job`.

.PARAMETER BlobName
The blob name to upload and reference from the Event Grid message.

.PARAMETER LocalFilePath
Optional path for the temporary CSV file to upload. If omitted, the script creates the file in the system temp directory.

.EXAMPLE
pwsh ./scripts/test-storage-process-function.ps1

Creates `test-file.csv`, uploads it to `incoming/test-file.csv`, and adds a `Microsoft.Storage.BlobCreated`
Event Grid message to the `storage-process-job` queue using Azurite.

.EXAMPLE
pwsh ./scripts/test-storage-process-function.ps1 -BlobName "subfolder/test-file.csv"

Uploads a blob with a nested path under the `incoming` container and enqueues the matching Event Grid message.

.EXAMPLE
pwsh ./scripts/test-storage-process-function.ps1 -ConnectionString "<connection-string>" -ContainerName "incoming" -QueueName "storage-process-job"

Uses an explicit storage account connection string instead of the local Azurite default.

.NOTES
Requirements:
- Azure CLI (`az`) installed
- the target storage account reachable (Azurite is good for local development)
- the storage-process function running locally if you want to observe end-to-end processing

Use `Get-Help ./scripts/test-storage-process-function.ps1 -Detailed` or `-Examples` to view this help.

.LINK
https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_comment_based_help?view=powershell-7.5
#>
param(
    [string]$ConnectionString = "UseDevelopmentStorage=true",
    [string]$ContainerName = "incoming",
    [string]$QueueName = "storage-process-job",
    [string]$BlobName = "test-file.csv",
    [string]$LocalFilePath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI ('az') is required to run this script."
}

if ([string]::IsNullOrWhiteSpace($LocalFilePath)) {
    $LocalFilePath = Join-Path ([System.IO.Path]::GetTempPath()) $BlobName
}

$directory = Split-Path -Parent $LocalFilePath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

# PDS test data https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir/pds-fhir-api-test-data#general-tests
$csvContent = @"
Id,GivenName,FamilyName,DOB,Postcode
1001,octavia,chislett,2008-09-20,
1002,beryl,shipperbottom,2011-05-09,KT21 1LJ
1003,briar,anderton,2009-02-15,KT21 1JA
1004,percy,gilman,2011-01-28,KT21 1EJ
1005,red,flindall,2011-05-17,KT20 6XJ
1006,monte,fielding,2008-05-21,KT21 1DJ
"@

Set-Content -Path $LocalFilePath -Value $csvContent -Encoding utf8NoBOM

Write-Host "Created test CSV at $LocalFilePath"

# Create the container if needed so repeated local test runs stay clean.
az storage container create `
    --name $ContainerName `
    --connection-string $ConnectionString `
    --only-show-errors | Out-Null

az storage queue create `
    --name $QueueName `
    --connection-string $ConnectionString `
    --only-show-errors | Out-Null

az storage blob upload `
    --container-name $ContainerName `
    --name $BlobName `
    --file $LocalFilePath `
    --overwrite true `
    --connection-string $ConnectionString `
    --only-show-errors | Out-Null

$eventTime = [DateTimeOffset]::UtcNow.ToString("o")
$blobUrl = "https://127.0.0.1:10000/devstoreaccount1/$ContainerName/$BlobName"
$message = @(
    @{
        id = [guid]::NewGuid().ToString()
        subject = "/blobServices/default/containers/$ContainerName/blobs/$BlobName"
        eventType = "Microsoft.Storage.BlobCreated"
        eventTime = $eventTime
        data = @{
            url = $blobUrl
        }
        dataVersion = "1"
        metadataVersion = "1"
    }
) | ConvertTo-Json -Compress -Depth 5

az storage message put `
    --queue-name $QueueName `
    --content $message `
    --connection-string $ConnectionString `
    --only-show-errors | Out-Null

Write-Host "Uploaded blob to $ContainerName/$BlobName"
Write-Host "Added queue message:"
Write-Host $message
