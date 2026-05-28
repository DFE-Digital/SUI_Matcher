@description('The location used for all deployed resources')
param location string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The resource ID of the subnet for private endpoints')
param peSubnetId string

@description('The resource ID of the virtual network for private DNS zone links')
param vnetId string

@minLength(3)
@description('The name of the existing storage account to use.')
param storageAccountName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

module storageResources './storage-resources.bicep' = {
  name: 'blob-event-processor-storage-resources'
  params: {
    location: location
    tags: tags
    storageAccountName: storageAccount.name
    storageAccountId: storageAccount.id
    peSubnetId: peSubnetId
    vnetId: vnetId
  }
}

output accountName string = storageAccount.name
output accountId string = storageAccount.id
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount.properties.primaryEndpoints.queue
output incomingContainerName string = storageResources.outputs.incomingContainerName
output processedContainerName string = storageResources.outputs.processedContainerName
output successContainerName string = storageResources.outputs.successContainerName
output queueName string = storageResources.outputs.queueName
output poisonQueueName string = storageResources.outputs.poisonQueueName
