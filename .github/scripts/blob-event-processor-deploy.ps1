#requires -Version 7.0

param (
    [string]$EnvironmentPrefix = "s215d01",
    [string]$ResourceGroupName = "s215d01-integration-blob-event-processor",
    [string]$TargetRegion = "northeurope",
    [bool]$ExcludeRoleAssignments = $true,
    [Parameter(Mandatory)][string]$MonitoringActionGroupEmail
)

# Build and publish container images
& {
    az configure --defaults acr=$containerRegistryName

    az acr build `
      --image "sui-client-storage-process-job:latest" `
      --file src/SUI.Client/SUI.Client.StorageProcessJob/Dockerfile `
      --build-arg MATCH_API_BASE_ADDRESS="https://example.com" `
      --build-arg CSV_DATE_FORMAT=yyyy-MM-dd `
      --build-arg CSV_COLUMN_ID=Id `
      --build-arg CSV_COLUMN_GIVEN=GivenName `
      --build-arg CSV_COLUMN_FAMILY=FamilyName `
      --build-arg CSV_COLUMN_BIRTH_DATE=DOB `
      --build-arg CSV_COLUMN_EMAIL=EMAIL `
      --build-arg CSV_COLUMN_POSTCODE=POSTCODE `
      --build-arg CSV_COLUMN_GENDER=GENDER `
      --build-arg CSV_COLUMN_PHONE=PHONE `
      --build-arg CSV_COLUMN_NHS_NUMBER=NHS_NUMBER `
      .
}

# Blob event processor stack
& {
    ## Define Azure deployment parameters
    $parameters = [PSCustomObject]@{
        environmentPrefix = $EnvironmentPrefix
        environmentName = "Integration"
        location = $TargetRegion
        monitoringActionGroupEmail = $MonitoringActionGroupEmail
        containerAppManagedEnvironmentNumber = "01"
        containerAppVnet = "192.168.0.0/16"
        containerAppEnvSubnet = "192.168.0.0/24"
        containerAppPeSubnet = "192.168.1.0/24"
        includeRoleAssignments = !$ExcludeRoleAssignments 
    }

    ## Write parameters to a file
    $tempFilePath = [System.IO.Path]::GetTempFileName()
    $parameters | Out-File 

    ## Complete what-if deployment, using parameters file
    $deploymentName = "$resourceGroupName-$TargetRegion-whatif"
    az deployment group create`
        --name "$deploymentName" `
        --location "$TargetRegion" `
        --resource-group "$ResourceGroupName" `
        --template-file "infra/stacks/blob-event-processor/main.bicep" `
        --parameters "@$tempFilePath"
}