targetScope = 'subscription'

@minLength(1)
@description('Name of the deployment environment used for stack resource naming')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources and the resource-group naming convention')
param environmentPrefix string

@description('The location used for all deployed resources')
param location string

var lowercaseEnvironmentName = toLower(environmentName)
var stackName = 'api-batch-processor'
var resourceGroupName = '${environmentPrefix}-${lowercaseEnvironmentName}-${stackName}'
var resourceGroupTags = {
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
  Stack: stackName
}

resource stackResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: resourceGroupTags
}

module stackDeployment 'main.bicep' = {
  name: '${stackName}-deployment'
  scope: stackResourceGroup
  params: {
    environmentName: environmentName
    environmentPrefix: environmentPrefix
    location: location
  }
}

output RESOURCE_GROUP_NAME string = stackResourceGroup.name
output RESOURCE_GROUP_ID string = stackResourceGroup.id
output STACK_NAME string = stackDeployment.outputs.STACK_NAME
output STACK_CONTRACT_STATUS string = stackDeployment.outputs.STACK_CONTRACT_STATUS
output LOCATION string = stackDeployment.outputs.LOCATION
output TAGS object = stackDeployment.outputs.TAGS
