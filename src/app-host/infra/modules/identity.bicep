@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Tags that will be applied to all resources')
param tags object = {}

module identity '../../../../infra/modules/shared/identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

output clientId string = identity.outputs.clientId
output name string = identity.outputs.name
output principalId string = identity.outputs.principalId
output id string = identity.outputs.id
