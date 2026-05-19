@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

var stackNameToken = empty(stackNameSuffix) ? '' : toLower(stackNameSuffix)

// The resource name can only contain alphanumeric characters.
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${environmentPrefix}${lowercaseEnvironmentName}${stackNameToken}acr01'
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

output endpoint string = containerRegistry.properties.loginServer
output name string = containerRegistry.name
