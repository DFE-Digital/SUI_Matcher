@description('The location used for all deployed resources')
param location string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The name of the Azure Container Registry to expose via a private endpoint')
param registryName string

@description('The resource ID of the subnet for the private endpoint')
param peSubnetId string

@description('The resource ID of the virtual network for the private DNS zone link')
param vnetId string

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2026-01-01-preview' existing = {
  name: registryName
}

resource acrPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: '${registryName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: peSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${registryName}-pe-conn'
        properties: {
          privateLinkServiceId: containerRegistry.id
          groupIds: [
            'registry'
          ]
        }
      }
    ]
  }
}

resource acrDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurecr.io'
  location: 'global'
  tags: tags
}

resource acrDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: acrDnsZone
  name: '${registryName}-dns-link'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnetId
    }
  }
}

resource acrDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = {
  parent: acrPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: replace(acrDnsZone.name, '.', '-')
        properties: {
          privateDnsZoneId: acrDnsZone.id
        }
      }
    ]
  }
}
