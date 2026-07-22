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
param location string = resourceGroup().location

@minLength(1)
@description('The email address to be used for monitoring alerts')
param monitoringActionGroupEmail string

@description('Turn on monitoring alerts')
param turnOnAlerts bool = false

@minLength(1)
@description('Container image tag for the GraphQL process job')
param graphqlProcessJobImageTag string = 'latest'

@minLength(1)
@description('Container image tag for the matching API')
param matchingApiImageTag string = 'latest'

@minLength(1)
@description('Container image tag for the external API')
param externalApiImageTag string = 'latest'

@allowed([
  'automatic'
  'manual'
])
@description('The deployment mode for the job: automatic (scheduled) or manual (triggered on-demand)')
param deploymentMode string = 'manual'

@description('The cron expression for the scheduled trigger when deploymentMode is automatic')
param cronExpression string = '0 9,12,15 * * 1-5'

@description('Whether or not to include role assignments, since some environments may restrict these.')
param includeRoleAssignments bool = true

@description('Optional value for the Environment tag when Azure Policy expects a different tag value than the deployment environment name.')
param tagEnvironmentName string = ''

@description('Optional additional tags to apply to deployed resources.')
param additionalTags object = {}

@description('Additional FQDNs allowed through the firewall, e.g., the GraphQL endpoint host')
param allowedGraphQLFqdns array = []

@secure()
@description('Runtime configuration values for the GraphQL process job.')
param graphqlProcessJobConfiguration object

var lowercaseEnvironmentName = toLower(environmentName)
var stackNameSuffix = 'abp'
var isProductionEnvironment = lowercaseEnvironmentName == 'prod' || lowercaseEnvironmentName == 'production'
var defaultNhsFqdns = isProductionEnvironment ? [
  'api.service.nhs.uk'
] : [
  'int.api.service.nhs.uk'
]
var allowedNhsFqdns = concat(defaultNhsFqdns, allowedGraphQLFqdns)
var effectiveTagEnvironmentName = empty(tagEnvironmentName) ? environmentName : tagEnvironmentName

var baseTags = {
  'azd-env-name': environmentName
  Product: 'SUI'
  Environment: effectiveTagEnvironmentName
  EnvironmentPrefix: environmentPrefix
  Stack: 'api-batch-processor'
}
var tags = union(baseTags, additionalTags)

var noHyphensEnvironmentPrefix = replace(environmentPrefix, '-', '')
var storageAccountName = toLower('${take(noHyphensEnvironmentPrefix, 8)}${take(lowercaseEnvironmentName, 8)}${stackNameSuffix}${take(uniqueString(resourceGroup().id, noHyphensEnvironmentPrefix, lowercaseEnvironmentName), 5)}')

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

module secrets '../../modules/shared/secrets.bicep' = {
  name: 'secrets'
  params: {
    location: location
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    stackNameSuffix: stackNameSuffix
    tags: tags
  }
}

module keyVaultPrivateEndpoint '../../modules/shared/key-vault-private-endpoint.bicep' = {
  name: 'secrets-private-endpoint'
  params: {
    location: location
    tags: tags
    keyVaultName: secrets.outputs.name
    peSubnetId: containerAppNetwork.outputs.privateEndpointSubnetId
    vnetId: containerAppNetwork.outputs.virtualNetworkId
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
    containerRegistryDataEndpointHostNames: containerRegistry.outputs.dataEndpointHostNames
    applicationInsightsIngestionHost: observability.outputs.applicationInsightsIngestionHost
    keyVaultName: secrets.outputs.name
    allowKeyVaultPublicEgress: false
    allowedNhsFqdns: allowedNhsFqdns
    logAnalyticsWorkspaceId: observability.outputs.workspaceId
    caeVnetAddressPrefixes: [
      containerAppVnet
    ]
    tags: tags
  }
}

module containerAppNetwork '../../modules/shared/container-app-network.bicep' = {
  name: 'container-app-network'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    stackNameSuffix: stackNameSuffix
    containerAppVnet: containerAppVnet
    privateEndpointSubnetAddressPrefix: containerAppPeSubnet
    tags: tags
  }
}

module caeFirewallPeering '../../modules/shared/virtual-network-peering.bicep' = {
  name: 'cae-firewall-peering'
  params: {
    vnet1Name: containerAppNetwork.outputs.virtualNetworkName
    vnet2Name: egressFirewall.outputs.firewallVnetName
    vnet1ToVnet2PeeringName: 'peering-fw-01'
    vnet2ToVnet1PeeringName: 'peering-cae-01'
    vnet1AllowForwardedTraffic: true
    vnet2AllowForwardedTraffic: true
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
    virtualNetworkName: containerAppNetwork.outputs.virtualNetworkName
    containerAppEnvSubnet: containerAppEnvSubnet
    tags: tags
    logAnalyticsWorkspaceName: observability.outputs.workspaceName
    routeTableId: egressFirewall.outputs.routeTableId
  }
  dependsOn: [
    caeFirewallPeering
  ]
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

module storage '../../modules/shared/storage-account.bicep' = {
  name: 'api-batch-processor-storage'
  params: {
    location: location
    storageAccountName: storageAccountName
    tags: tags
  }
}

module storagePrivateEndpoint '../../modules/shared/storage-private-endpoints.bicep' = {
  name: 'api-batch-processor-storage-private-endpoint'
  params: {
    location: location
    tags: tags
    storageAccountName: storage.outputs.accountName
    storageAccountId: storage.outputs.accountId
    storageServices: [
      'blob'
    ]
    peSubnetId: containerAppNetwork.outputs.privateEndpointSubnetId
    vnetId: containerAppNetwork.outputs.virtualNetworkId
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
  dependsOn: [
    keyVaultPrivateEndpoint
  ]
}

module graphqlProcessJob '../../modules/api-batch-processor/graphql-process-job.bicep' = {
  name: 'api-batch-processor-graphql-process-job'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    containerAppsEnvironmentId: containerAppEnvironment.outputs.id
    managedIdentityId: identity.outputs.id
    managedIdentityClientId: identity.outputs.clientId
    containerRegistryServer: containerRegistry.outputs.endpoint
    imageTag: graphqlProcessJobImageTag
    matchApiBaseAddress: 'https://matching-api.internal.${containerAppEnvironment.outputs.defaultDomain}'
    applicationInsightsConnectionString: observability.outputs.applicationInsightsConnectionString
    graphqlProcessJobConfiguration: graphqlProcessJobConfiguration
    deploymentMode: deploymentMode
    cronExpression: cronExpression
    tags: tags
  }
}

output STACK_NAME string = 'api-batch-processor'
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
output STORAGE_ACCOUNT_NAME string = storage.outputs.accountName
output STORAGE_ACCOUNT_ID string = storage.outputs.accountId
output STORAGE_BLOB_ENDPOINT string = storage.outputs.blobEndpoint
output GRAPHQL_PROCESS_JOB_NAME string = graphqlProcessJob.outputs.jobName
output GRAPHQL_PROCESS_JOB_ID string = graphqlProcessJob.outputs.jobId
output MATCHING_API_NAME string = matchingApi.outputs.name
output MATCHING_API_ID string = matchingApi.outputs.id
output EXTERNAL_API_NAME string = externalApi.outputs.name
output EXTERNAL_API_ID string = externalApi.outputs.id
