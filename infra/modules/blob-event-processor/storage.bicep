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
    publicNetworkAccess: 'Enabled' // Must be Enabled to allow Trusted Microsoft Services when networkAcls are used
    supportsHttpsTrafficOnly: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
    }
  }
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
