# How to run e2e tests on client infra

## Pre reqs

* Access to the azure console
* Membership of the keyvault admin group

## Client Infra

There is a job to build the client infra. It will build the infra within this directory which is meant to mimic a local authority server where this client will run.
There is also information in `README_Deployment.md` which requires extra manual steps to deploy.

## Upload Client

We need to upload any new client to blob storage so it can be collected by the VM we created when we deployed the client infra. Again there is a job to do this.

In addition, we also deploy versions manually to [Nuget.org](https://www.nuget.org/packages?q=SUI.*.Watcher&includeComputedFrameworks=true&prerel=true&sortby=relevance) which can be downloaded via the dotnet tools command.

## Connectivity

The VM is hosted on a subnet but it needs connectivity to ACA and Blob storage. We need to create private link resources on this subnet for these resources so our host can connect. This needs to be done in the console due to access restrictions. Make a note of the ACA private link address. Also, create a SAS token for your blob storage account to allow access. Store this too.

## Login to VM

Based on access restrictions in DfE we are not allowed to deploy Azure Bastion. We also use BYOD which means we don't have direct line of sight to any VM in order to SSH or RDP to it. This means we need to use the serial console which isn't the best...

To login to the VM you need to go to the running VM in the resource group -> Help -> Serial console.

This will spin up a window and give you access to their SAC (Special Administration Console), from here we can jump to CMD and then to powershell.

```
cmd
```
Should create a new channel
```
ch -si 1
```
Should prompt you to login. This is were you will need access to read keyvault. In keyvault you will see two VM related secrets. Please use those to access the host. Leave domain blank.

Once at command prompt you can drop into powershell
```
powershell
```

Now we are setup and ready to perform some testing.

## Connectivity Tests

First we should test we can resolve and connect to our dependencies. Taking the addresses we stored earlier we should run this for both.

```
nslookup <ADDRESS>
tnc <ADDRESS> -port <PORT>
```

If this looks all good we can move on to the next steps.

## Download AzCopy and Get Client

We need a way to pull files from blob storage. 

```
curl.exe -L -o AzCopy.zip https://aka.ms/downloadazcopy-v10-windows
 
# Expand Archive
Expand-Archive ./AzCopy.zip ./AzCopy -Force
 
# Move AzCopy to the destination you want to store it
mkdir "C:\Users\Sui\AzCopy\"
cd "C:\Users\Sui\AzCopy\"
Get-ChildItem ./AzCopy/*/azcopy.exe | Move-Item -Destination "C:\Users\AzCopy\AzCopy.exe"

# Get Client
$storageUri = "<STORAGE_URI+TOKEN>"

.\azcopy.exe cp $storageUri .  --recursive=true
```

Here we have downloaded the azcopy exe and have used it to connect to our blob storage and download our client.

## Test Server

Some powershell commands to do a simple test.
```
$uri = "<PRIVATELINK_ADDRESS>/matching/api/v1/matchperson"
$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add('Content-Type','Application/Json')
$hash = @{  given    = "octavia"; 
            family = "chislett"
            birthdate = "2008-09-20"
            }

$JSON = $hash | convertto-json 

$result = Invoke-WebRequest -uri $uri  -Headers $headers -Method POST -Body $JSON -UseBasicParsing
```

## Test Client and Server

Now we want to test the client and the server. 

```
$processeddir = "<PROCESSED_DIR>"
$unprocesseddir "<UNPROCESSED_DIR>"

mkdir $processeddir
mkdir $unprocesseddir

$json_data = @(
    [PSCustomObject]@{
        MatchApiBaseAddress = "<PRIVATELINK_ADDRESS>"
    }
)

$json_data | ConvertTo-Json | Out-File -FilePath "<WORKING_DIR>\appsettings.json"

$json_file_contents = Get-Content -Path "<WORKING_DIR>\appsettings.json"

Start-Job -ScriptBlock { <WORKED_DIR>\suiw.exe --input <UNPROCESSED_DIR> --output <PROCESSED_DIR> }
```
This will start the client in the background

Next we create some data files for testing and verify they are what we want.

```
$csv_data = @(
    [PSCustomObject]@{
        GivenName = "octavia"
        Surname = "chislett"
        Email = "test@test.com"
        DOB = "2008-09-20"
    }
)

$csv_data | Export-CSV -Path "<UNPROCESSED_DIR>\data.csv" -NoTypeInformation

$csv_file_contents = Get-Content -Path "<UNPROCESSED_DIR>\data.csv"
```

This should run the test and we will see a data output file in the processed directory. We should verify that is what we expect.

```
$completed_file_contents = Get-Content -Path "<PROCESSED_DIR>\<OUTPUT_FILE>.csv"
```
