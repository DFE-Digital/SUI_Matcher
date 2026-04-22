targetScope = 'resourceGroup'

@minLength(1)
@description('The name of the deployment environment')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The location used for all deployed resources')
param location string = resourceGroup().location

var tags = {
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
  Stack: 'api-batch-processor'
}

// Placeholder stack root. Architecture-specific resources will be added in follow-up tickets.
output STACK_NAME string = 'api-batch-processor'
output STACK_CONTRACT_STATUS string = 'placeholder'
output LOCATION string = location
output TAGS object = tags
