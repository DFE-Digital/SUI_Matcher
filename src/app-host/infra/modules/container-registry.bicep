@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Tags that will be applied to all resources')
param tags object = {}

module containerRegistry '../../../../infra/modules/shared/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

output endpoint string = containerRegistry.outputs.endpoint
output name string = containerRegistry.outputs.name
