#requires -Version 7.0

param (
    [string]$EnvironmentPrefix = "s215d01",
    [string]$ResourceGroupName = "s215d01-integration-blob-event-processor",
    [string]$TargetRegion = "northeurope",
    [bool]$ExcludeRoleAssignments = $true,
    [Parameter(Mandatory)][string]$MonitoringActionGroupEmail
)

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
    az deployment group what-if `
        --name "$deploymentName" `
        --location "$TargetRegion" `
        --resource-group "$ResourceGroupName" `
        --template-file "infra/stacks/blob-event-processor/main.bicep" `
        --parameters "@$tempFilePath"
}