@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Tags that will be applied to all resources')
param tags object = {}

var incomingContainerName = 'incoming'
var processedContainerName = 'processed'
var successContainerName = 'success'
var queueName = 'storage-process-job'
var poisonQueueName = '${queueName}-poison'
var storageAccountName = toLower('${take(environmentPrefix, 8)}${take(lowercaseEnvironmentName, 8)}bep${take(uniqueString(resourceGroup().id, environmentPrefix, lowercaseEnvironmentName), 5)}')

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  tags: tags
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for containerName in [
    incomingContainerName
    processedContainerName
    successContainerName
  ]: {
    parent: blobService
    name: containerName
    properties: {
      publicAccess: 'None'
    }
  }
]

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource queues 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = [
  for currentQueueName in [
    queueName
    poisonQueueName
  ]: {
    parent: queueService
    name: currentQueueName
  }
]

output accountName string = storageAccount.name
output accountId string = storageAccount.id
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output queueEndpoint string = storageAccount.properties.primaryEndpoints.queue
output incomingContainerName string = incomingContainerName
output processedContainerName string = processedContainerName
output successContainerName string = successContainerName
output queueName string = queueName
output poisonQueueName string = poisonQueueName
