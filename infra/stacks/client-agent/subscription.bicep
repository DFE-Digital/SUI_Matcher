targetScope = 'subscription'

@minLength(1)
@description('Name of the deployment environment used for stack resource naming')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources and the resource-group naming convention')
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

@description('Network')
param clientNetwork string = '192.168.0.128/25'

@description('Subnet Range')
param clientSubnetRange string = '192.168.0.128/26'

var lowercaseEnvironmentName = toLower(environmentName)
var stackName = 'client-agent'
var resourceGroupName = '${environmentPrefix}-${lowercaseEnvironmentName}-${stackName}'
var resourceGroupTags = {
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
  Stack: stackName
}

resource stackResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: resourceGroupTags
}

module stackDeployment 'main.bicep' = {
  name: '${stackName}-deployment'
  scope: stackResourceGroup
  params: {
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    adminUsername: adminUsername
    adminPassword: adminPassword
    containerAppManagedEnvironmentNumber: containerAppManagedEnvironmentNumber
    containerAppVnet: containerAppVnet
    containerAppEnvSubnet: containerAppEnvSubnet
    location: location
    monitoringActionGroupEmail: monitoringActionGroupEmail
    turnOnAlerts: turnOnAlerts
    clientNetwork: clientNetwork
    clientSubnetRange: clientSubnetRange
  }
}

output RESOURCE_GROUP_NAME string = stackResourceGroup.name
output RESOURCE_GROUP_ID string = stackResourceGroup.id
output MANAGED_IDENTITY_CLIENT_ID string = stackDeployment.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = stackDeployment.outputs.MANAGED_IDENTITY_NAME
output MANAGED_IDENTITY_PRINCIPAL_ID string = stackDeployment.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = stackDeployment.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = stackDeployment.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = stackDeployment.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = stackDeployment.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_NAME string = stackDeployment.outputs.AZURE_CONTAINER_REGISTRY_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = stackDeployment.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = stackDeployment.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = stackDeployment.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output APPLICATION_INSIGHTS_CONNECTION_STRING string = stackDeployment.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING
output SECRETS_VAULTURI string = stackDeployment.outputs.SECRETS_VAULTURI
output SECRETS_VAULT_NAME string = stackDeployment.outputs.SECRETS_VAULT_NAME
output CLIENT_VM_NAME string = stackDeployment.outputs.CLIENT_VM_NAME
output CLIENT_VIRTUAL_NETWORK_NAME string = stackDeployment.outputs.CLIENT_VIRTUAL_NETWORK_NAME
output CLIENT_FIREWALL_NAME string = stackDeployment.outputs.CLIENT_FIREWALL_NAME
output CLIENT_ROUTE_TABLE_NAME string = stackDeployment.outputs.CLIENT_ROUTE_TABLE_NAME
