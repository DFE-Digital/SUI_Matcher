@description('The location used for all deployed resources')
param location string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The name of the storage account to connect to')
param storageAccountName string

@description('The resource ID of the storage account to connect to')
param storageAccountId string

@description('Storage service group IDs to expose privately, such as blob or queue')
param storageServices array

@description('The resource ID of the subnet for private endpoints')
param peSubnetId string

@description('The resource ID of the virtual network for private DNS zone links')
param vnetId string

var dnsZoneNames = [
  for storageService in storageServices: 'privatelink.${storageService}.${environment().suffixes.storage}'
]

resource privateEndpoints 'Microsoft.Network/privateEndpoints@2023-05-01' = [
  for storageService in storageServices: {
    name: '${storageAccountName}-${storageService}-pe'
    location: location
    tags: tags
    properties: {
      subnet: {
        id: peSubnetId
      }
      privateLinkServiceConnections: [
        {
          name: '${storageAccountName}-${storageService}-pe-conn'
          properties: {
            privateLinkServiceId: storageAccountId
            groupIds: [
              storageService
            ]
          }
        }
      ]
    }
  }
]

resource dnsZones 'Microsoft.Network/privateDnsZones@2020-06-01' = [
  for dnsZoneName in dnsZoneNames: {
    name: dnsZoneName
    location: 'global'
    tags: tags
  }
]

resource dnsZoneLinks 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = [
  for (storageService, serviceIndex) in storageServices: {
    parent: dnsZones[serviceIndex]
    name: '${storageAccountName}-${storageService}-dns-link'
    location: 'global'
    tags: tags
    properties: {
      registrationEnabled: false
      virtualNetwork: {
        id: vnetId
      }
    }
  }
]

resource dnsZoneGroups 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = [
  for (dnsZoneName, serviceIndex) in dnsZoneNames: {
    parent: privateEndpoints[serviceIndex]
    name: 'default'
    properties: {
      privateDnsZoneConfigs: [
        {
          name: replace(dnsZoneName, '.', '-')
          properties: {
            privateDnsZoneId: dnsZones[serviceIndex].id
          }
        }
      ]
    }
  }
]
