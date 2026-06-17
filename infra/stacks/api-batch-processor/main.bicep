targetScope = 'resourceGroup'

@minLength(1)
@description('Name of the deployment environment used for stack resource naming')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The location used for all deployed resources')
param location string = resourceGroup().location

@minLength(1)
@description('The address prefix for the virtual network')
param containerAppVnet string

@minLength(1)
@description('The address prefix for the private endpoint subnet')
param containerAppPeSubnet string

var lowercaseEnvironmentName = toLower(environmentName)
var stackNameSuffix = 'abp'
var noHyphensEnvironmentPrefix = replace(environmentPrefix, '-', '')
var storageAccountName = toLower('${take(noHyphensEnvironmentPrefix, 8)}${take(lowercaseEnvironmentName, 8)}${stackNameSuffix}${take(uniqueString(resourceGroup().id, noHyphensEnvironmentPrefix, lowercaseEnvironmentName), 5)}')
var tags = {
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
  Stack: 'api-batch-processor'
}

module storage '../../modules/shared/storage-account.bicep' = {
  name: 'api-batch-processor-storage'
  params: {
    location: location
    storageAccountName: storageAccountName
    tags: tags
  }
}

module containerAppNetwork '../../modules/shared/container-app-network.bicep' = {
  name: 'container-app-network'
  params: {
    location: location
    environmentPrefix: environmentPrefix
    lowercaseEnvironmentName: lowercaseEnvironmentName
    stackNameSuffix: stackNameSuffix
    containerAppVnet: containerAppVnet
    privateEndpointSubnetAddressPrefix: containerAppPeSubnet
    tags: tags
  }
}

module storagePrivateEndpoint '../../modules/shared/storage-private-endpoints.bicep' = {
  name: 'api-batch-processor-storage-private-endpoint'
  params: {
    location: location
    tags: tags
    storageAccountName: storage.outputs.accountName
    storageAccountId: storage.outputs.accountId
    storageServices: [
      'blob'
    ]
    peSubnetId: containerAppNetwork.outputs.privateEndpointSubnetId
    vnetId: containerAppNetwork.outputs.virtualNetworkId
  }
}

output STACK_NAME string = 'api-batch-processor'
output LOCATION string = location
output TAGS object = tags
output STORAGE_ACCOUNT_NAME string = storage.outputs.accountName
output STORAGE_ACCOUNT_ID string = storage.outputs.accountId
output STORAGE_BLOB_ENDPOINT string = storage.outputs.blobEndpoint
