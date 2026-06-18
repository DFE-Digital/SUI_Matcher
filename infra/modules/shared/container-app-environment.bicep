@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('container app managed environment number')
param containerAppManagedEnvironmentNumber string

@description('The name of the virtual network that contains the container app environment subnet. When empty, the module creates the virtual network for backwards compatibility.')
param virtualNetworkName string = ''

@description('The address prefix for the virtual network. Required when virtualNetworkName is empty.')
param containerAppVnet string = ''

@description('Container App environment subnet')
param containerAppEnvSubnet string

@description('Optional private endpoint subnet address prefix')
param privateEndpointSubnetAddressPrefix string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

// Backwards compatibility with legacy in app-host/infra
@description('Optional resource ID of the route table to attach to the container app environment subnet so that egress traffic flows through a firewall. When empty, no route table is attached.')
param routeTableId string = ''

@description('The Log Analytics workspace name used by the container app environment')
param logAnalyticsWorkspaceName string

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var caeVnetName = empty(virtualNetworkName)
  ? '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-vnet-cae-01'
  : virtualNetworkName
var containerAppEnvironmentSubnetName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-subnet-cae-01'
var privateEndpointSubnetName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-subnet-pe-01'
var caeSubnetRouteTable = empty(routeTableId)
  ? {}
  : {
      routeTable: {
        id: routeTableId
      }
    }

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource caeVnet 'Microsoft.Network/virtualNetworks@2024-05-01' = if (empty(virtualNetworkName)) {
  name: caeVnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        containerAppVnet
      ]
    }
  }
}

resource containerAppEnvironmentSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  #disable-next-line use-parent-property
  name: '${caeVnetName}/${containerAppEnvironmentSubnetName}'
  properties: union(
    {
      addressPrefix: containerAppEnvSubnet
      delegations: [
        {
          name: 'Microsoft.App.environments'
          properties: {
            serviceName: 'Microsoft.App/environments'
          }
        }
      ]
    },
    caeSubnetRouteTable
  )
  dependsOn: [
    caeVnet
  ]
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

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-cae-${containerAppManagedEnvironmentNumber}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'default'
        workloadProfileType: 'D4'
        minimumCount: 1
        maximumCount: 1
      }
    ]
    peerTrafficConfiguration: {
      encryption: {
        enabled: true
      }
    }
    publicNetworkAccess: 'Disabled'
    vnetConfiguration: {
      infrastructureSubnetId: containerAppEnvironmentSubnet.id
      internal: true
    }
  }
}

output name string = containerAppEnvironment.name
output id string = containerAppEnvironment.id
output defaultDomain string = containerAppEnvironment.properties.defaultDomain
output virtualNetworkName string = caeVnetName
output virtualNetworkId string = resourceId('Microsoft.Network/virtualNetworks', caeVnetName)
output privateEndpointSubnetId string = empty(privateEndpointSubnetAddressPrefix)
  ? ''
  : resourceId('Microsoft.Network/virtualNetworks/subnets', caeVnetName, privateEndpointSubnetName)
