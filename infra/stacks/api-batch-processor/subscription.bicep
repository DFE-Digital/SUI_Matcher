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

@allowed([
  'create'
  'existing'
])
@description('Whether the subscription wrapper should create the stack resource group or deploy into an existing one.')
param resourceGroupMode string = 'create'

@description('The existing resource group name to deploy into when resourceGroupMode is existing.')
param targetResourceGroupName string = ''

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
var stackName = 'api-batch-processor'
var stackResourceGroupName = '${environmentPrefix}-${lowercaseEnvironmentName}-${stackName}'
var deploymentResourceGroupName = resourceGroupMode == 'existing' ? targetResourceGroupName : stackResourceGroupName
var effectiveTagEnvironmentName = empty(tagEnvironmentName) ? environmentName : tagEnvironmentName
var baseResourceGroupTags = {
  Product: 'SUI'
  Environment: effectiveTagEnvironmentName
  EnvironmentPrefix: environmentPrefix
  Stack: stackName
}
var resourceGroupTags = union(baseResourceGroupTags, additionalTags)

resource stackResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = if (resourceGroupMode == 'create') {
  name: stackResourceGroupName
  location: location
  tags: resourceGroupTags
}

module stackDeployment 'main.bicep' = {
  name: '${stackName}-deployment'
  scope: resourceGroup(deploymentResourceGroupName)
  params: {
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    containerAppManagedEnvironmentNumber: containerAppManagedEnvironmentNumber
    containerAppVnet: containerAppVnet
    containerAppEnvSubnet: containerAppEnvSubnet
    containerAppPeSubnet: containerAppPeSubnet
    location: location
    monitoringActionGroupEmail: monitoringActionGroupEmail
    turnOnAlerts: turnOnAlerts
    graphqlProcessJobImageTag: graphqlProcessJobImageTag
    matchingApiImageTag: matchingApiImageTag
    externalApiImageTag: externalApiImageTag
    deploymentMode: deploymentMode
    cronExpression: cronExpression
    includeRoleAssignments: includeRoleAssignments
    tagEnvironmentName: tagEnvironmentName
    additionalTags: additionalTags
    allowedGraphQLFqdns: allowedGraphQLFqdns
    graphqlProcessJobConfiguration: graphqlProcessJobConfiguration
  }
  dependsOn: [
    stackResourceGroup
  ]
}

output RESOURCE_GROUP_NAME string = deploymentResourceGroupName
output RESOURCE_GROUP_ID string = subscriptionResourceId('Microsoft.Resources/resourceGroups', deploymentResourceGroupName)
output STACK_NAME string = stackDeployment.outputs.STACK_NAME
output LOCATION string = stackDeployment.outputs.LOCATION
output TAGS object = stackDeployment.outputs.TAGS
output MANAGED_IDENTITY_CLIENT_ID string = stackDeployment.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = stackDeployment.outputs.MANAGED_IDENTITY_NAME
output MANAGED_IDENTITY_PRINCIPAL_ID string = stackDeployment.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = stackDeployment.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = stackDeployment.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = stackDeployment.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_DATA_ENDPOINT_HOST_NAMES array = stackDeployment.outputs.AZURE_CONTAINER_REGISTRY_DATA_ENDPOINT_HOST_NAMES
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
output GRAPHQL_PROCESS_JOB_NAME string = stackDeployment.outputs.GRAPHQL_PROCESS_JOB_NAME
output GRAPHQL_PROCESS_JOB_ID string = stackDeployment.outputs.GRAPHQL_PROCESS_JOB_ID
output MATCHING_API_NAME string = stackDeployment.outputs.MATCHING_API_NAME
output MATCHING_API_ID string = stackDeployment.outputs.MATCHING_API_ID
output EXTERNAL_API_NAME string = stackDeployment.outputs.EXTERNAL_API_NAME
output EXTERNAL_API_ID string = stackDeployment.outputs.EXTERNAL_API_ID
