targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('Username for the Virtual Machine.')
param adminUsername string

@description('Password for the Virtual Machine.')
@minLength(12)
@secure()
param adminPassword string

@minLength(1)
@description('The version number of the container app managed environment, used for naming convention')
param containerAppManagedEnvironmentNumber string

@minLength(1)
@description('The address prefix for the virtual network')
param containerAppVnet string

@minLength(1)
@description('Container App environment subnet')
param containerAppEnvSubnet string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The email address to be used for monitoring alerts')
param monitoringActionGroupEmail string

@description('Turn on monitoring alerts')
param turnOnAlerts bool = false

@description('Enable Audit Logging feature for Aspire application')
// Preserved for compatibility with the existing audit deployment docs and parameter surface.
#disable-next-line no-unused-params
param enableAuditLogging bool = false

@description('Network')
param clientNetwork string = '192.168.0.128/25'

@description('Subnet Range')
param clientSubnetRange string = '192.168.0.128/26'

var lowercaseEnvironmentName = toLower(environmentName)
var tags = {
  'azd-env-name': environmentName
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
  Stack: 'client-agent'
}

module identity '../../modules/shared/identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

module containerRegistry '../../modules/shared/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

module observability '../../modules/shared/observability.bicep' = {
  name: 'observability'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

module containerAppEnvironment '../../modules/shared/container-app-environment.bicep' = {
  name: 'container-app-environment'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    containerAppManagedEnvironmentNumber: containerAppManagedEnvironmentNumber
    containerAppVnet: containerAppVnet
    containerAppEnvSubnet: containerAppEnvSubnet
    tags: tags
    logAnalyticsWorkspaceName: observability.outputs.workspaceName
  }
}

module secrets '../../modules/shared/secrets.bicep' = {
  name: 'secrets'
  params: {
    location: location
    environmentName: environmentName
    environmentPrefix: environmentPrefix
  }
}

module monitoring '../../modules/shared/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    turnOnAlerts: turnOnAlerts
    logAnalyticsWorkspaceId: observability.outputs.workspaceId
    actionGroupEmail: monitoringActionGroupEmail
  }
}

module clientInfrastructure '../../modules/client-agent/infrastructure.bicep' = {
  name: 'client-agent-infrastructure'
  params: {
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    adminUsername: adminUsername
    adminPassword: adminPassword
    network: clientNetwork
    subnetRange: clientSubnetRange
    location: location
    containerAppEnvironmentVnetName: containerAppEnvironment.outputs.virtualNetworkName
    containerAppEnvironmentResourceGroupName: resourceGroup().name
    logAnalyticsWorkspaceName: observability.outputs.workspaceName
    logAnalyticsWorkspaceResourceGroupName: resourceGroup().name
    containerRegistryEndpoint: containerRegistry.outputs.endpoint
    keyVaultName: secrets.outputs.name
    keyVaultEndpoint: '${secrets.outputs.name}.vault.azure.net'
    tags: tags
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = identity.outputs.clientId
output MANAGED_IDENTITY_NAME string = identity.outputs.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = identity.outputs.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = observability.outputs.workspaceName
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = observability.outputs.workspaceId
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.endpoint
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = identity.outputs.id
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppEnvironment.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.outputs.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.outputs.defaultDomain
output APPLICATION_INSIGHTS_CONNECTION_STRING string = observability.outputs.applicationInsightsConnectionString
output SECRETS_VAULTURI string = secrets.outputs.vaultUri
output SECRETS_VAULT_NAME string = secrets.outputs.name
output CLIENT_VM_NAME string = clientInfrastructure.outputs.vmName
output CLIENT_VIRTUAL_NETWORK_NAME string = clientInfrastructure.outputs.clientVirtualNetworkName
output CLIENT_FIREWALL_NAME string = clientInfrastructure.outputs.firewallName
output CLIENT_ROUTE_TABLE_NAME string = clientInfrastructure.outputs.routeTableName
