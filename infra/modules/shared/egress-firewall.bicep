@description('The location used for all deployed resources')
param location string

@description('Name of the deployment environment')
param environmentName string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('The login server for the container registry to allow through the firewall')
param containerRegistryEndpoint string

@description('The name of the shared Key Vault')
param keyVaultName string

@description('The Key Vault FQDN allowed through the firewall')
#disable-next-line no-hardcoded-env-urls
param keyVaultEndpoint string = '${keyVaultName}.vault.azure.net'

@description('The address prefix for the firewall virtual network')
param firewallVnetAddressPrefix string = '192.168.4.0/23'

@description('The address prefix for the Azure Firewall subnet')
param firewallSubnetAddressPrefix string = '192.168.4.0/25'

@description('The address spaces of the CAE virtual network (used as source address ranges in the firewall policy)')
param caeVnetAddressPrefixes array

@description('Tags that will be applied to all resources')
param tags object = {}

var lowercaseEnvironmentName = toLower(environmentName)
var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'

var firewallVnetName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-vnetfw-01'
var firewallName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-vnetfw-Firewall'
var firewallPolicyName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-fwp-01'
var publicIpName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-pib-01'
var routeTableName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-rt-01'

resource firewallVirtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: firewallVnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        firewallVnetAddressPrefix
      ]
    }
    encryption: {
      enabled: false
      enforcement: 'AllowUnencrypted'
    }
    privateEndpointVNetPolicies: 'Disabled'
    subnets: [
      {
        name: 'AzureFirewallSubnet'
        properties: {
          addressPrefix: firewallSubnetAddressPrefix
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
    ]
    enableDdosProtection: false
  }
}

resource publicIP 'Microsoft.Network/publicIPAddresses@2023-06-01' = {
  name: publicIpName
  location: location
  sku: {
    name: 'Standard'
  }
  tags: tags
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource firewallPolicy 'Microsoft.Network/firewallPolicies@2022-01-01' = {
  name: firewallPolicyName
  location: location
  tags: tags
  properties: {
    sku: {
      tier: 'Basic'
    }
    threatIntelMode: 'Alert'
  }
}

resource firewall 'Microsoft.Network/azureFirewalls@2024-05-01' = {
  name: firewallName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'AZFW_VNet'
      tier: 'Basic'
    }
    threatIntelMode: 'Alert'
    firewallPolicy: {
      id: firewallPolicy.id
    }
    ipConfigurations: [
      {
        name: 'fw-ip-config-01'
        properties: {
          publicIPAddress: {
            id: publicIP.id
          }
          subnet: {
            id: '${firewallVirtualNetwork.id}/subnets/AzureFirewallSubnet'
          }
        }
      }
    ]
  }
}

resource applicationRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2022-01-01' = {
  parent: firewallPolicy
  name: 'DefaultApplicationRuleCollectionGroup'
  properties: {
    priority: 300
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        action: {
          type: 'Allow'
        }
        name: 'Global-rules-arc'
        priority: 300
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'nhs-api-allow'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'int.api.service.nhs.uk'
              'api.service.nhs.uk'
            ]
            terminateTLS: false
            sourceAddresses: caeVnetAddressPrefixes
          }
        ]
      }
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        action: {
          type: 'Allow'
        }
        name: 'allow-system-arc'
        priority: 200
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'acr-allow'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              containerRegistryEndpoint
            ]
            terminateTLS: false
            sourceAddresses: caeVnetAddressPrefixes
          }
          {
            ruleType: 'ApplicationRule'
            name: 'kv-allow'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              keyVaultEndpoint
            ]
            terminateTLS: false
            sourceAddresses: caeVnetAddressPrefixes
          }
          {
            ruleType: 'ApplicationRule'
            name: 'login-allow'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'login.microsoftonline.com'
            ]
            terminateTLS: false
            sourceAddresses: caeVnetAddressPrefixes
          }
        ]
      }
    ]
  }
}

resource routeTable 'Microsoft.Network/routeTables@2024-05-01' = {
  name: routeTableName
  location: location
  tags: tags
  properties: {
    disableBgpRoutePropagation: false
    routes: [
      {
        name: 'DefaultToFirewall'
        properties: {
          addressPrefix: '0.0.0.0/0'
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: firewall.properties.ipConfigurations[0].properties.privateIPAddress
        }
        type: 'Microsoft.Network/routeTables/routes'
      }
    ]
  }
}

output routeTableId string = routeTable.id
output routeTableName string = routeTable.name
output firewallPrivateIp string = firewall.properties.ipConfigurations[0].properties.privateIPAddress
output firewallVnetName string = firewallVirtualNetwork.name
output firewallVnetId string = firewallVirtualNetwork.id
