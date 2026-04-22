@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('container app managed environment number')
param containerAppManagedEnvironmentNumber string

@description('The address prefix for the virtual network')
param containerAppVnet string

@description('Container App environment subnet')
param containerAppEnvSubnet string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The Log Analytics workspace name used by the container app environment')
param logAnalyticsWorkspaceName string

module containerAppEnvironment '../../../../infra/modules/shared/container-app-environment.bicep' = {
  name: 'container-app-environment'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    containerAppManagedEnvironmentNumber: containerAppManagedEnvironmentNumber
    containerAppVnet: containerAppVnet
    containerAppEnvSubnet: containerAppEnvSubnet
    tags: tags
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

output name string = containerAppEnvironment.outputs.name
output id string = containerAppEnvironment.outputs.id
output defaultDomain string = containerAppEnvironment.outputs.defaultDomain
