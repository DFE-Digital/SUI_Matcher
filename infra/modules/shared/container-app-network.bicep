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

@allowed([
  'create'
  'existing'
])
@description('Whether the module should create the virtual network or reuse one that already exists in the target resource group. Use existing when the virtual network carries configuration managed outside this stack, such as manually created peerings, that a virtual network write would otherwise remove.')
param virtualNetworkMode string = 'create'

@description('Tags that will be applied to all resources')
param tags object = {}

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var privateEndpointSubnetName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-subnet-pe-01'
var caeVnetName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-vnet-cae-01'

// A virtual network write replaces the whole resource, so peerings that are not declared here are removed.
// When virtualNetworkMode is existing the write is skipped and only the subnets below are managed, which
// leaves peerings created outside this stack in place. Both modes use the same name, so existing mode
// reuses the network a previous create-mode deployment made.
resource caeVnet 'Microsoft.Network/virtualNetworks@2024-05-01' = if (virtualNetworkMode == 'create') {
  name: caeVnetName
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
  #disable-next-line use-parent-property
  name: '${caeVnetName}/${privateEndpointSubnetName}'
  properties: {
    addressPrefix: privateEndpointSubnetAddressPrefix
  }
  dependsOn: [
    caeVnet
  ]
}

output virtualNetworkName string = caeVnetName
output virtualNetworkId string = resourceId('Microsoft.Network/virtualNetworks', caeVnetName)
output privateEndpointSubnetId string = empty(privateEndpointSubnetAddressPrefix)
  ? ''
  : resourceId('Microsoft.Network/virtualNetworks/subnets', caeVnetName, privateEndpointSubnetName)
