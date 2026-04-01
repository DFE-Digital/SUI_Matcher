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

$csvContent = @"
Given,Family,BirthDate,Gender,AddressPostalCode
Jane,Doe,2012-05-10,Female,SW1A1AA
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
