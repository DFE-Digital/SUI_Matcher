@description('environmentName')
param environmentName string = 'integration'

@description('Username for the Virtual Machine.')
param adminUsername string

@description('Password for the Virtual Machine.')
@minLength(12)
@secure()
param adminPassword string

@description('environmentPrefix')
param environmentPrefix string = 's215d01'

@description('Network')
param network string = '192.168.0.128/25'

@description('Subnet Range')
param subnetRange string = '192.168.0.128/26'

param location string = resourceGroup().location

param virtualNetworksVnetfwName string = '${environmentPrefix}-vnetfw-01'

param logAnalyticsWorkspaceName string = '${environmentPrefix}-${environmentName}-loganalytics-01'

param vnetFirewallName string = '${environmentPrefix}-vnetfw-Firewall'

param routeTablesIntegrationRtName01 string = '${environmentPrefix}-${toLower(environmentName)}-rt-01'

param dbsClientConsoleApplogsEndpointName string = 'DbsClientConsoleApplogsEndpoint'

param dbsClientConsoleAppLogsRuleName string = 'DbsClientConsoleAppLogsRule'

@description('Tags for the resources')
param paramTags object = {
  Product: 'SUI'
  Environment: 'Dev'
  'Service Offering': 'SUI'
}

var lowercaseEnvironmentName = toLower(environmentName)

module clientInfrastructure '../../../../infra/modules/client-agent/infrastructure.bicep' = {
  name: 'client-infrastructure'
  params: {
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    adminUsername: adminUsername
    adminPassword: adminPassword
    network: network
    subnetRange: subnetRange
    location: location
    containerAppEnvironmentVnetName: '${environmentPrefix}-${lowercaseEnvironmentName}-vnet-cae-01'
    containerAppEnvironmentResourceGroupName: '${environmentPrefix}-${lowercaseEnvironmentName}'
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    logAnalyticsWorkspaceResourceGroupName: '${environmentPrefix}-${lowercaseEnvironmentName}'
    containerRegistryEndpoint: '${environmentPrefix}${lowercaseEnvironmentName}acr01.azurecr.io'
    keyVaultName: '${environmentPrefix}-int-kv01'
    keyVaultEndpoint: '${environmentPrefix}-int-kv01.vault.azure.net'
    virtualNetworksVnetfwName: virtualNetworksVnetfwName
    vnetFirewallName: vnetFirewallName
    routeTablesIntegrationRtName01: routeTablesIntegrationRtName01
    dbsClientConsoleApplogsEndpointName: dbsClientConsoleApplogsEndpointName
    dbsClientConsoleAppLogsRuleName: dbsClientConsoleAppLogsRuleName
    tags: paramTags
  }
}
