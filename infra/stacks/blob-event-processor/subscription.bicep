targetScope = 'subscription'

@minLength(1)
@description('Name of the deployment environment used for stack resource naming')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources and the resource-group naming convention')
param environmentPrefix string

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

var lowercaseEnvironmentName = toLower(environmentName)
var stackName = 'blob-event-processor'
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
    containerAppManagedEnvironmentNumber: containerAppManagedEnvironmentNumber
    containerAppVnet: containerAppVnet
    containerAppEnvSubnet: containerAppEnvSubnet
    location: location
    monitoringActionGroupEmail: monitoringActionGroupEmail
    turnOnAlerts: turnOnAlerts
  }
}

output RESOURCE_GROUP_NAME string = stackResourceGroup.name
output RESOURCE_GROUP_ID string = stackResourceGroup.id
output STACK_NAME string = stackDeployment.outputs.STACK_NAME
output LOCATION string = stackDeployment.outputs.LOCATION
output TAGS object = stackDeployment.outputs.TAGS
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
output STORAGE_ACCOUNT_NAME string = stackDeployment.outputs.STORAGE_ACCOUNT_NAME
output STORAGE_ACCOUNT_ID string = stackDeployment.outputs.STORAGE_ACCOUNT_ID
output STORAGE_BLOB_ENDPOINT string = stackDeployment.outputs.STORAGE_BLOB_ENDPOINT
output STORAGE_QUEUE_ENDPOINT string = stackDeployment.outputs.STORAGE_QUEUE_ENDPOINT
output STORAGE_INCOMING_CONTAINER_NAME string = stackDeployment.outputs.STORAGE_INCOMING_CONTAINER_NAME
output STORAGE_PROCESSED_CONTAINER_NAME string = stackDeployment.outputs.STORAGE_PROCESSED_CONTAINER_NAME
output STORAGE_SUCCESS_CONTAINER_NAME string = stackDeployment.outputs.STORAGE_SUCCESS_CONTAINER_NAME
output STORAGE_QUEUE_NAME string = stackDeployment.outputs.STORAGE_QUEUE_NAME
output STORAGE_POISON_QUEUE_NAME string = stackDeployment.outputs.STORAGE_POISON_QUEUE_NAME
output EVENT_GRID_SYSTEM_TOPIC_NAME string = stackDeployment.outputs.EVENT_GRID_SYSTEM_TOPIC_NAME
output EVENT_GRID_SYSTEM_TOPIC_ID string = stackDeployment.outputs.EVENT_GRID_SYSTEM_TOPIC_ID
output EVENT_GRID_EVENT_SUBSCRIPTION_NAME string = stackDeployment.outputs.EVENT_GRID_EVENT_SUBSCRIPTION_NAME
output EVENT_GRID_EVENT_SUBSCRIPTION_ID string = stackDeployment.outputs.EVENT_GRID_EVENT_SUBSCRIPTION_ID
