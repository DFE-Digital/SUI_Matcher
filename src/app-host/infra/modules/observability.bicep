@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Tags that will be applied to all resources')
param tags object = {}

module observability '../../../../infra/modules/shared/observability.bicep' = {
  name: 'observability'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    tags: tags
  }
}

output workspaceName string = observability.outputs.workspaceName
output workspaceId string = observability.outputs.workspaceId
output applicationInsightsConnectionString string = observability.outputs.applicationInsightsConnectionString
