targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the deployment environment used for stack resource naming')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources')
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

var tags = {
  'azd-env-name': environmentName
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
  Stack: 'blob-event-processor'
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

module storage '../../modules/blob-event-processor/storage.bicep' = {
  name: 'blob-event-processor-storage'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

module eventGrid '../../modules/blob-event-processor/event-grid.bicep' = {
  name: 'blob-event-processor-event-grid'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    storageAccountId: storage.outputs.accountId
    queueName: storage.outputs.queueName
    incomingContainerName: storage.outputs.incomingContainerName
    tags: tags
  }
}

output STACK_NAME string = 'blob-event-processor'
output LOCATION string = location
output TAGS object = tags
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
output STORAGE_ACCOUNT_NAME string = storage.outputs.accountName
output STORAGE_ACCOUNT_ID string = storage.outputs.accountId
output STORAGE_BLOB_ENDPOINT string = storage.outputs.blobEndpoint
output STORAGE_QUEUE_ENDPOINT string = storage.outputs.queueEndpoint
output STORAGE_INCOMING_CONTAINER_NAME string = storage.outputs.incomingContainerName
output STORAGE_PROCESSED_CONTAINER_NAME string = storage.outputs.processedContainerName
output STORAGE_SUCCESS_CONTAINER_NAME string = storage.outputs.successContainerName
output STORAGE_QUEUE_NAME string = storage.outputs.queueName
output STORAGE_POISON_QUEUE_NAME string = storage.outputs.poisonQueueName
output EVENT_GRID_SYSTEM_TOPIC_NAME string = eventGrid.outputs.systemTopicName
output EVENT_GRID_SYSTEM_TOPIC_ID string = eventGrid.outputs.systemTopicId
output EVENT_GRID_EVENT_SUBSCRIPTION_NAME string = eventGrid.outputs.eventSubscriptionName
output EVENT_GRID_EVENT_SUBSCRIPTION_ID string = eventGrid.outputs.eventSubscriptionId
