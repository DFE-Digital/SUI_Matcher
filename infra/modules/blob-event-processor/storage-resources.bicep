@description('The location used for all deployed resources')
param location string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The name of the storage account to configure')
param storageAccountName string

@description('The resource ID of the storage account to configure')
param storageAccountId string

@description('The resource ID of the subnet for private endpoints')
param peSubnetId string

@description('The resource ID of the virtual network for private DNS zone links')
param vnetId string

var incomingContainerName = 'incoming'
var processedContainerName = 'processed'
var successContainerName = 'success'
var queueName = 'storage-process-job'
var poisonQueueName = '${queueName}-poison'
var containerNames = [
  incomingContainerName
  processedContainerName
  successContainerName
]
var queueNames = [
  queueName
  poisonQueueName
]

module blobContainers '../shared/storage-blob-containers.bicep' = {
  name: 'blob-event-processor-storage-containers'
  params: {
    storageAccountName: storageAccountName
    containerNames: containerNames
  }
}

module storageQueues '../shared/storage-queues.bicep' = {
  name: 'blob-event-processor-storage-queues'
  params: {
    storageAccountName: storageAccountName
    queueNames: queueNames
  }
}

module storagePrivateEndpoints '../shared/storage-private-endpoints.bicep' = {
  name: 'blob-event-processor-storage-private-endpoints'
  params: {
    location: location
    tags: tags
    storageAccountName: storageAccountName
    storageAccountId: storageAccountId
    storageServices: [
      'blob'
      'queue'
    ]
    peSubnetId: peSubnetId
    vnetId: vnetId
  }
}

output incomingContainerName string = incomingContainerName
output processedContainerName string = processedContainerName
output successContainerName string = successContainerName
output queueName string = queueName
output poisonQueueName string = poisonQueueName
