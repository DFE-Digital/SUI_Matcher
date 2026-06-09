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

@description('The dedicated data endpoint host names for the Premium container registry to allow through the firewall')
param containerRegistryDataEndpointHostNames array

@description('The Application Insights ingestion host allowed through the firewall')
param applicationInsightsIngestionHost string

@description('The name of the shared Key Vault')
param keyVaultName string

@description('The Key Vault FQDN allowed through the firewall')
#disable-next-line no-hardcoded-env-urls
param keyVaultEndpoint string = '${keyVaultName}.vault.azure.net'

@description('Whether to allow public Key Vault egress through the firewall. Set to false when the stack uses a Key Vault private endpoint with private DNS.')
param allowKeyVaultPublicEgress bool = true

@description('The NHS API FQDNs allowed through the firewall for this environment')
param allowedNhsFqdns array

@description('Resource ID of the Log Analytics workspace that receives firewall diagnostic logs and metrics')
param logAnalyticsWorkspaceId string

@description('The address prefix for the firewall virtual network')
param firewallVnetAddressPrefix string = '192.168.1.0/24'

@description('The address prefix for the Azure Firewall subnet')
param firewallSubnetAddressPrefix string = '192.168.1.0/26'

@description('The address prefix for the Azure Firewall management subnet (required for Basic SKU)')
param firewallManagementSubnetAddressPrefix string = '192.168.1.64/26'

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
var managementPublicIpName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-pib-mgmt-01'
var routeTableName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-rt-01'
var containerAppRegion = toLower(replace(location, ' ', ''))

var platformFqdnRules = [
  {
    name: 'mcr-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: 'mcr.microsoft.com'
  }
  {
    name: 'mcr-data-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: '*.data.mcr.microsoft.com'
  }
  {
    name: 'aks-packages-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: 'packages.aks.azure.com'
  }
  {
    name: 'aks-mirror-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: 'acs-mirror.azureedge.net'
  }
  {
    name: 'acr-allow'
    fqdn: containerRegistryEndpoint
  }
  {
    name: 'login-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: 'login.microsoft.com'
  }
  {
    name: 'login-online-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: 'login.microsoftonline.com'
  }
  {
    name: 'login-online-wildcard-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: '*.login.microsoftonline.com'
  }
  {
    name: 'login-wildcard-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: '*.login.microsoft.com'
  }
  {
    name: 'managed-identity-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: '*.identity.azure.net'
  }
  {
    name: 'monitor-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: '${containerAppRegion}.livediagnostics.monitor.azure.com'
  }
  {
    name: 'app-insights-allow'
    fqdn: applicationInsightsIngestionHost
  }
  {
    name: 'visualstudio-allow'
    #disable-next-line no-hardcoded-env-urls
    fqdn: 'dc.services.visualstudio.com'
  }
]

var containerRegistryDataEndpointFqdnRules = [
  for (hostName, index) in containerRegistryDataEndpointHostNames: {
    name: index == 0 ? 'acr-data-allow' : 'acr-data-${index + 1}-allow'
    fqdn: hostName
  }
]

var keyVaultFqdnRules = allowKeyVaultPublicEgress
  ? [
      {
        name: 'kv-allow'
        fqdn: keyVaultEndpoint
      }
    ]
  : []

var systemFqdnRules = concat(platformFqdnRules, containerRegistryDataEndpointFqdnRules, keyVaultFqdnRules)

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
    enableDdosProtection: false
  }
}

resource firewallSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: firewallVirtualNetwork
  name: 'AzureFirewallSubnet'
  properties: {
    addressPrefix: firewallSubnetAddressPrefix
  }
}

resource firewallManagementSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: firewallVirtualNetwork
  name: 'AzureFirewallManagementSubnet'
  properties: {
    addressPrefix: firewallManagementSubnetAddressPrefix
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

resource managementPublicIP 'Microsoft.Network/publicIPAddresses@2023-06-01' = {
  name: managementPublicIpName
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
            id: firewallSubnet.id
          }
        }
      }
    ]
    managementIpConfiguration: {
      name: 'fw-mgmt-ip-config-01'
      properties: {
        publicIPAddress: {
          id: managementPublicIP.id
        }
        subnet: {
          id: firewallManagementSubnet.id
        }
      }
    }
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
        name: 'allow-nhs-fqdns'
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
            targetFqdns: [for fqdn in allowedNhsFqdns: fqdn]
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
        name: 'allow-sui-system-fqds'
        priority: 200
        rules: [
          for rule in systemFqdnRules: {
            ruleType: 'ApplicationRule'
            name: rule.name
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              rule.fqdn
            ]
            terminateTLS: false
            sourceAddresses: caeVnetAddressPrefixes
          }
        ]
      }
    ]
  }
}

resource networkRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2022-01-01' = {
  parent: firewallPolicy
  name: 'DefaultNetworkRuleCollectionGroup'
  properties: {
    priority: 400
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        action: {
          type: 'Allow'
        }
        name: 'allow-platform-network-nrc'
        priority: 200
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'azure-dns-allow'
            ipProtocols: [
              'TCP'
              'UDP'
            ]
            sourceAddresses: caeVnetAddressPrefixes
            destinationAddresses: [
              '168.63.129.16'
            ]
            destinationPorts: [
              '53'
            ]
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
    disableBgpRoutePropagation: true
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

resource firewallDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${firewallName}-diagnostics'
  scope: firewall
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'AllLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output routeTableId string = routeTable.id
output routeTableName string = routeTable.name
output firewallName string = firewall.name
output firewallPrivateIp string = firewall.properties.ipConfigurations[0].properties.privateIPAddress
output firewallVnetName string = firewallVirtualNetwork.name
output firewallVnetId string = firewallVirtualNetwork.id
