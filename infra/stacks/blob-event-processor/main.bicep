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
@description('The address prefix for the private endpoint subnet')
param containerAppPeSubnet string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The email address to be used for monitoring alerts')
param monitoringActionGroupEmail string

@description('Turn on monitoring alerts')
param turnOnAlerts bool = false

@minLength(1)
@description('Container image tag for the storage process job')
param storageProcessJobImageTag string = 'latest'

@minLength(1)
@description('Container image tag for the matching API')
param matchingApiImageTag string = 'latest'

@minLength(1)
@description('Container image tag for the external API')
param externalApiImageTag string = 'latest'

@description('Whether or not to include role assignments, since some environments may restrict these.')
param includeRoleAssignments bool = true

@allowed([
  'create'
  'existing'
])
@description('Whether the stack should create its storage account or use an existing account in the target resource group.')
param storageAccountMode string = 'create'

@description('The name of the existing storage account to use when storageAccountMode is existing.')
param existingStorageAccountName string = ''

var lowercaseEnvironmentName = toLower(environmentName)
var stackNameSuffix = 'bep'

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
    stackNameSuffix: stackNameSuffix
    includeRoleAssignments: includeRoleAssignments
    containerRegistryName: containerRegistry.outputs.name
    tags: tags
  }
}

module containerRegistry '../../modules/shared/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    stackNameSuffix: stackNameSuffix
    tags: tags
  }
}

module observability '../../modules/shared/observability.bicep' = {
  name: 'observability'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    stackNameSuffix: stackNameSuffix
    tags: tags
  }
}

module egressFirewall '../../modules/shared/egress-firewall.bicep' = {
  name: 'egress-firewall'
  params: {
    location: location
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    stackNameSuffix: stackNameSuffix
    containerRegistryEndpoint: containerRegistry.outputs.endpoint
    keyVaultName: secrets.outputs.name
    caeVnetAddressPrefixes: [
      containerAppVnet
    ]
    tags: tags
  }
}

module containerAppEnvironment '../../modules/shared/container-app-environment.bicep' = {
  name: 'container-app-environment'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    stackNameSuffix: stackNameSuffix
    containerAppManagedEnvironmentNumber: containerAppManagedEnvironmentNumber
    containerAppVnet: containerAppVnet
    containerAppEnvSubnet: containerAppEnvSubnet
    privateEndpointSubnetAddressPrefix: containerAppPeSubnet
    tags: tags
    logAnalyticsWorkspaceName: observability.outputs.workspaceName
    routeTableId: egressFirewall.outputs.routeTableId
  }
}

module caeFirewallPeering '../../modules/shared/virtual-network-peering.bicep' = {
  name: 'cae-firewall-peering'
  params: {
    vnet1Name: containerAppEnvironment.outputs.virtualNetworkName
    vnet2Name: egressFirewall.outputs.firewallVnetName
    vnet1ToVnet2PeeringName: 'peering-fw-01'
    vnet2ToVnet1PeeringName: 'peering-cae-01'
  }
}

module secrets '../../modules/shared/secrets.bicep' = {
  name: 'secrets'
  params: {
    location: location
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    stackNameSuffix: stackNameSuffix
  }
}

module keyVaultPrivateEndpoint '../../modules/shared/key-vault-private-endpoint.bicep' = {
  name: 'secrets-private-endpoint'
  params: {
    location: location
    tags: tags
    keyVaultName: secrets.outputs.name
    peSubnetId: containerAppEnvironment.outputs.privateEndpointSubnetId
    vnetId: containerAppEnvironment.outputs.virtualNetworkId
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

module createdStorage '../../modules/blob-event-processor/storage.bicep' = if (storageAccountMode == 'create') {
  name: 'blob-event-processor-storage'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
    peSubnetId: containerAppEnvironment.outputs.privateEndpointSubnetId
    vnetId: containerAppEnvironment.outputs.virtualNetworkId
  }
}

module existingStorage '../../modules/blob-event-processor/existing-storage.bicep' = if (storageAccountMode == 'existing') {
  name: 'blob-event-processor-existing-storage'
  params: {
    location: location
    tags: tags
    peSubnetId: containerAppEnvironment.outputs.privateEndpointSubnetId
    vnetId: containerAppEnvironment.outputs.virtualNetworkId
    storageAccountName: existingStorageAccountName
  }
}

var storageAccountName = storageAccountMode == 'create' ? createdStorage!.outputs.accountName : existingStorage!.outputs.accountName
var storageAccountId = storageAccountMode == 'create' ? createdStorage!.outputs.accountId : existingStorage!.outputs.accountId
var storageBlobEndpoint = storageAccountMode == 'create' ? createdStorage!.outputs.blobEndpoint : existingStorage!.outputs.blobEndpoint
var storageQueueEndpoint = storageAccountMode == 'create' ? createdStorage!.outputs.queueEndpoint : existingStorage!.outputs.queueEndpoint
var storageIncomingContainerName = storageAccountMode == 'create' ? createdStorage!.outputs.incomingContainerName : existingStorage!.outputs.incomingContainerName
var storageProcessedContainerName = storageAccountMode == 'create' ? createdStorage!.outputs.processedContainerName : existingStorage!.outputs.processedContainerName
var storageSuccessContainerName = storageAccountMode == 'create' ? createdStorage!.outputs.successContainerName : existingStorage!.outputs.successContainerName
var storageQueueName = storageAccountMode == 'create' ? createdStorage!.outputs.queueName : existingStorage!.outputs.queueName
var storagePoisonQueueName = storageAccountMode == 'create' ? createdStorage!.outputs.poisonQueueName : existingStorage!.outputs.poisonQueueName

module eventGrid '../../modules/blob-event-processor/event-grid.bicep' = {
  name: 'blob-event-processor-event-grid'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    storageAccountId: storageAccountId
    queueName: storageQueueName
    incomingContainerName: storageIncomingContainerName
    tags: tags
  }
}

module storageProcessJob '../../modules/blob-event-processor/container-app-job.bicep' = {
  name: 'blob-event-processor-storage-process-job'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    containerAppsEnvironmentId: containerAppEnvironment.outputs.id
    managedIdentityId: identity.outputs.id
    managedIdentityPrincipalId: identity.outputs.principalId
    managedIdentityClientId: identity.outputs.clientId
    storageAccountName: storageAccountName
    queueName: storageQueueName
    blobServiceUri: storageBlobEndpoint
    queueServiceUri: storageQueueEndpoint
    containerRegistryServer: containerRegistry.outputs.endpoint
    imageTag: storageProcessJobImageTag
    matchApiBaseAddress: 'https://matching-api.internal.${containerAppEnvironment.outputs.defaultDomain}'
    tags: tags
    includeRoleAssignments: includeRoleAssignments
  }
}

module matchingApi '../../modules/api-apps/matching-api.bicep' = {
  name: 'matching-api'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppEnvironment.outputs.id
    containerAppsEnvironmentDefaultDomain: containerAppEnvironment.outputs.defaultDomain
    containerRegistryServer: containerRegistry.outputs.endpoint
    managedIdentityId: identity.outputs.id
    managedIdentityClientId: identity.outputs.clientId
    environmentName: environmentName
    imageTag: matchingApiImageTag
    applicationInsightsConnectionString: observability.outputs.applicationInsightsConnectionString
    tags: tags
  }
}

module externalApi '../../modules/api-apps/external-api.bicep' = {
  name: 'external-api'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppEnvironment.outputs.id
    containerRegistryServer: containerRegistry.outputs.endpoint
    managedIdentityId: identity.outputs.id
    managedIdentityPrincipalId: identity.outputs.principalId
    managedIdentityClientId: identity.outputs.clientId
    environmentName: environmentName
    imageTag: externalApiImageTag
    keyVaultName: secrets.outputs.name
    keyVaultUri: secrets.outputs.vaultUri
    applicationInsightsConnectionString: observability.outputs.applicationInsightsConnectionString
    tags: tags
    includeRoleAssignments: includeRoleAssignments
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
output AZURE_CONTAINER_REGISTRY_DATA_ENDPOINT_HOST_NAMES array = containerRegistry.outputs.dataEndpointHostNames
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = identity.outputs.id
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppEnvironment.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.outputs.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.outputs.defaultDomain
output APPLICATION_INSIGHTS_CONNECTION_STRING string = observability.outputs.applicationInsightsConnectionString
output SECRETS_VAULTURI string = secrets.outputs.vaultUri
output SECRETS_VAULT_NAME string = secrets.outputs.name
output STORAGE_ACCOUNT_NAME string = storageAccountName
output STORAGE_ACCOUNT_ID string = storageAccountId
output STORAGE_BLOB_ENDPOINT string = storageBlobEndpoint
output STORAGE_QUEUE_ENDPOINT string = storageQueueEndpoint
output STORAGE_INCOMING_CONTAINER_NAME string = storageIncomingContainerName
output STORAGE_PROCESSED_CONTAINER_NAME string = storageProcessedContainerName
output STORAGE_SUCCESS_CONTAINER_NAME string = storageSuccessContainerName
output STORAGE_QUEUE_NAME string = storageQueueName
output STORAGE_POISON_QUEUE_NAME string = storagePoisonQueueName
output EVENT_GRID_SYSTEM_TOPIC_NAME string = eventGrid.outputs.systemTopicName
output EVENT_GRID_SYSTEM_TOPIC_ID string = eventGrid.outputs.systemTopicId
output EVENT_GRID_EVENT_SUBSCRIPTION_NAME string = eventGrid.outputs.eventSubscriptionName
output EVENT_GRID_EVENT_SUBSCRIPTION_ID string = eventGrid.outputs.eventSubscriptionId
output STORAGE_PROCESS_JOB_NAME string = storageProcessJob.outputs.jobName
output STORAGE_PROCESS_JOB_ID string = storageProcessJob.outputs.jobId
output MATCHING_API_NAME string = matchingApi.outputs.name
output MATCHING_API_ID string = matchingApi.outputs.id
output EXTERNAL_API_NAME string = externalApi.outputs.name
output EXTERNAL_API_ID string = externalApi.outputs.id
