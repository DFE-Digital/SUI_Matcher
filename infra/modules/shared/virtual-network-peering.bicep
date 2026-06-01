@description('The name of the first virtual network')
param vnet1Name string

@description('The name of the second virtual network')
param vnet2Name string

@description('The name of the peering created on vnet1 (pointing at vnet2)')
param vnet1ToVnet2PeeringName string = 'peering-to-${vnet2Name}'

@description('The name of the peering created on vnet2 (pointing at vnet1)')
param vnet2ToVnet1PeeringName string = 'peering-to-${vnet1Name}'

resource vnet1 'Microsoft.Network/virtualNetworks@2024-05-01' existing = {
  name: vnet1Name
}

resource vnet2 'Microsoft.Network/virtualNetworks@2024-05-01' existing = {
  name: vnet2Name
}

resource vnet1ToVnet2Peering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2024-05-01' = {
  parent: vnet1
  name: vnet1ToVnet2PeeringName
  properties: {
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: false
    allowGatewayTransit: false
    useRemoteGateways: false
    remoteVirtualNetwork: {
      id: vnet2.id
    }
  }
}

resource vnet2ToVnet1Peering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2024-05-01' = {
  parent: vnet2
  name: vnet2ToVnet1PeeringName
  properties: {
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: false
    allowGatewayTransit: false
    useRemoteGateways: false
    remoteVirtualNetwork: {
      id: vnet1.id
    }
  }
}
