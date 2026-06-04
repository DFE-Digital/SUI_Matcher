@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('The address prefix for the virtual network')
param containerAppVnet string

@description('Optional private endpoint subnet address prefix')
param privateEndpointSubnetAddressPrefix string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var privateEndpointSubnetName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-subnet-pe-01'

resource caeVnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-vnet-cae-01'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        containerAppVnet
      ]
    }
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = if (!empty(privateEndpointSubnetAddressPrefix)) {
  parent: caeVnet
  name: privateEndpointSubnetName
  properties: {
    addressPrefix: privateEndpointSubnetAddressPrefix
  }
}

output virtualNetworkName string = caeVnet.name
output virtualNetworkId string = caeVnet.id
output privateEndpointSubnetId string = empty(privateEndpointSubnetAddressPrefix) ? '' : '${caeVnet.id}/subnets/${privateEndpointSubnetName}'
