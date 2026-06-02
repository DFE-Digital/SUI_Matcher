@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('Whether or not to include role assignments, since some environments may restrict these.')
param includeRoleAssignments bool = true

@description('The container registry resource name to grant this identity AcrPull on. Leave empty to skip the grant.')
param containerRegistryName string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-mi-01'
  location: location
  tags: tags
}

// empty name check for backwards compatibility with legacy deploy
module acrPullRbac '../shared/acr-pull-rbac.bicep' = if (includeRoleAssignments && !empty(containerRegistryName)) {
  name: 'identity-acr-pull-rbac'
  params: {
    containerRegistryName: containerRegistryName
    principalId: managedIdentity.properties.principalId
  }
}

output clientId string = managedIdentity.properties.clientId
output name string = managedIdentity.name
output principalId string = managedIdentity.properties.principalId
output id string = managedIdentity.id
