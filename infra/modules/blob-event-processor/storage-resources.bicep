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

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  name: '${storageAccountName}/default'
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for containerName in containerNames: {
    parent: blobService
    name: containerName
    properties: {
      defaultEncryptionScope: '$account-encryption-key'
      denyEncryptionScopeOverride: false
      publicAccess: 'None'
    }
  }
]

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' existing = {
  name: '${storageAccountName}/default'
}

resource queues 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = [
  for currentQueueName in queueNames: {
    parent: queueService
    name: currentQueueName
  }
]

resource blobPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: '${storageAccountName}-blob-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: peSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${storageAccountName}-blob-pe-conn'
        properties: {
          privateLinkServiceId: storageAccountId
          groupIds: [
            'blob'
          ]
        }
      }
    ]
  }
}

resource queuePrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: '${storageAccountName}-queue-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: peSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${storageAccountName}-queue-pe-conn'
        properties: {
          privateLinkServiceId: storageAccountId
          groupIds: [
            'queue'
          ]
        }
      }
    ]
  }
}

resource blobDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.${environment().suffixes.storage}'
  location: 'global'
  tags: tags
}

resource queueDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.queue.${environment().suffixes.storage}'
  location: 'global'
  tags: tags
}

resource blobDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: blobDnsZone
  name: '${storageAccountName}-blob-dns-link'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource queueDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: queueDnsZone
  name: '${storageAccountName}-queue-dns-link'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource blobDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = {
  parent: blobPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: replace(blobDnsZone.name, '.', '-')
        properties: {
          privateDnsZoneId: blobDnsZone.id
        }
      }
    ]
  }
}

resource queueDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = {
  parent: queuePrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: replace(queueDnsZone.name, '.', '-')
        properties: {
          privateDnsZoneId: queueDnsZone.id
        }
      }
    ]
  }
}

output incomingContainerName string = incomingContainerName
output processedContainerName string = processedContainerName
output successContainerName string = successContainerName
output queueName string = queueName
output poisonQueueName string = poisonQueueName
