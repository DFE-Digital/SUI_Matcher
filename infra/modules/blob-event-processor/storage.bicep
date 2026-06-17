@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The resource ID of the subnet for private endpoints')
param peSubnetId string

@description('The resource ID of the virtual network for private DNS zone links')
param vnetId string

var noHyphensEnvironmentPrefix = replace(environmentPrefix, '-', '')
var storageAccountName = toLower('${take(noHyphensEnvironmentPrefix, 8)}${take(lowercaseEnvironmentName, 8)}bep${take(uniqueString(resourceGroup().id, noHyphensEnvironmentPrefix, lowercaseEnvironmentName), 5)}')

module storageAccount '../shared/storage-account.bicep' = {
  name: 'blob-event-processor-storage-account'
  params: {
    location: location
    storageAccountName: storageAccountName
    tags: tags
  }
}

module storageResources './storage-resources.bicep' = {
  name: 'blob-event-processor-storage-resources'
  params: {
    location: location
    tags: tags
    storageAccountName: storageAccount.outputs.accountName
    storageAccountId: storageAccount.outputs.accountId
    peSubnetId: peSubnetId
    vnetId: vnetId
  }
}

output accountName string = storageAccount.outputs.accountName
output accountId string = storageAccount.outputs.accountId
output blobEndpoint string = storageAccount.outputs.blobEndpoint
output queueEndpoint string = storageAccount.outputs.queueEndpoint
output incomingContainerName string = storageResources.outputs.incomingContainerName
output processedContainerName string = storageResources.outputs.processedContainerName
output successContainerName string = storageResources.outputs.successContainerName
output queueName string = storageResources.outputs.queueName
output poisonQueueName string = storageResources.outputs.poisonQueueName
