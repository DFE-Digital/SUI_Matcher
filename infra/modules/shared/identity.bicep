@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Tags that will be applied to all resources')
param tags object = {}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-mi-01'
  location: location
  tags: tags
}

output clientId string = managedIdentity.properties.clientId
output name string = managedIdentity.name
output principalId string = managedIdentity.properties.principalId
output id string = managedIdentity.id
