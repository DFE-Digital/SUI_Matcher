@description('environmentName')
param environmentName string

@description('environmentPrefix')
param environmentPrefix string

@description('Username for the Virtual Machine.')
param adminUsername string

@description('Password for the Virtual Machine.')
@minLength(12)
@secure()
param adminPassword string

@description('Network')
param network string = '192.168.0.128/25'

@description('Subnet Range')
param subnetRange string = '192.168.0.128/26'

@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('The name of the container app environment virtual network')
param containerAppEnvironmentVnetName string

@description('The resource group that contains the container app environment virtual network')
param containerAppEnvironmentResourceGroupName string = resourceGroup().name

@description('The name of the shared Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('The resource group that contains the shared Log Analytics workspace')
param logAnalyticsWorkspaceResourceGroupName string = resourceGroup().name

@description('The login server for the shared container registry')
param containerRegistryEndpoint string

@description('The name of the shared Key Vault')
param keyVaultName string = '${environmentPrefix}-int-kv01'

@description('The Key Vault FQDN allowed through the client-agent firewall')
param keyVaultEndpoint string = '${keyVaultName}.vault.azure.net'

param virtualNetworksVnetfwName string = '${environmentPrefix}-vnetfw-01'

param vnetFirewallName string = '${environmentPrefix}-vnetfw-Firewall'

param routeTablesIntegrationRtName01 string = '${environmentPrefix}-${toLower(environmentName)}-rt-01'

param dbsClientConsoleApplogsEndpointName string = 'DbsClientConsoleApplogsEndpoint'

param dbsClientConsoleAppLogsRuleName string = 'DbsClientConsoleAppLogsRule'

@description('Tags for the resources')
param tags object = {
  Product: 'SUI'
  Environment: 'Dev'
  'Service Offering': 'SUI'
}

resource caeVnet 'Microsoft.Network/virtualNetworks@2022-07-01' existing = {
  scope: resourceGroup(containerAppEnvironmentResourceGroupName)
  name: containerAppEnvironmentVnetName
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  scope: resourceGroup(logAnalyticsWorkspaceResourceGroupName)
  name: logAnalyticsWorkspaceName
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-02-01' = {
  name: '${environmentPrefix}-${environmentName}-clientvnet-01'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        network
      ]
    }
    subnets: [
      {
        name: '${environmentPrefix}-${environmentName}-clientsubnet-01'
        properties: {
          addressPrefix: subnetRange
        }
      }
    ]
  }
}

resource nic 'Microsoft.Network/networkInterfaces@2021-02-01' = {
  name: '${environmentPrefix}-${environmentName}-nic-01'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: {
            id: vnet.properties.subnets[0].id
          }
          privateIPAllocationMethod: 'Dynamic'
        }
      }
    ]
  }
}

resource vm 'Microsoft.Compute/virtualMachines@2022-03-01' = {
  name: '${environmentPrefix}-${environmentName}-vm-01'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    hardwareProfile: {
      vmSize: 'Standard_DS2_v2'
    }
    osProfile: {
      computerName: '${environmentPrefix}-vm-01'
      adminUsername: adminUsername
      adminPassword: adminPassword
      windowsConfiguration: {
        provisionVMAgent: true
        enableAutomaticUpdates: true
        patchSettings: {
          patchMode: 'AutomaticByOS'
          assessmentMode: 'AutomaticByPlatform'
        }
      }
    }
    storageProfile: {
      imageReference: {
        publisher: 'MicrosoftWindowsServer'
        offer: 'WindowsServer'
        sku: '2022-Datacenter'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
      }
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: nic.id
        }
      ]
    }
  }
}

resource firewallVirtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: virtualNetworksVnetfwName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '192.168.2.0/23'
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
          addressPrefixes: [
            '192.168.2.0/25'
          ]
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
    ]
    enableDdosProtection: false
  }
}

resource caeToFirewallPeering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2020-05-01' = {
  name: '${caeVnet.name}/peering-fw-01'
  properties: {
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: false
    allowGatewayTransit: false
    useRemoteGateways: false
    remoteVirtualNetwork: {
      id: firewallVirtualNetwork.id
    }
  }
}

resource publicIP 'Microsoft.Network/publicIPAddresses@2023-06-01' = {
  name: '${environmentPrefix}-${environmentName}-pib-01'
  location: location
  sku: {
    name: 'Standard'
  }
  tags: tags
  properties: {
    publicIPAllocationMethod: 'static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource firewallPolicy 'Microsoft.Network/firewallPolicies@2022-01-01' = {
  name: '${environmentPrefix}-${environmentName}-fwp-01'
  location: location
  tags: tags
  properties: {
    threatIntelMode: 'Alert'
  }
}

resource firewall 'Microsoft.Network/azureFirewalls@2024-05-01' = {
  name: vnetFirewallName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'AZFW_VNet'
      tier: 'Standard'
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
            name: 'global-rule-01'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'int.api.service.nhs.uk'
            ]
            terminateTLS: false
            sourceAddresses: [...caeVnet.properties.addressSpace.addressPrefixes]
          }
        ]
      }
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        action: {
          type: 'Allow'
        }
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
            sourceAddresses: [
              '192.168.0.0/24'
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Nuget'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'api.nuget.org'
            ]
            terminateTLS: false
            sourceAddresses: [
              '192.168.0.0/24'
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'kv'
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
            sourceAddresses: [
              '192.168.0.0/24'
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'login'
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
            sourceAddresses: [
              '192.168.0.0/24'
            ]
          }
        ]
        name: 'allow-acr'
        priority: 200
      }
    ]
  }
}

resource routeTable 'Microsoft.Network/routeTables@2024-05-01' = {
  name: routeTablesIntegrationRtName01
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

resource dbsClientConsoleApplogsEndpoint 'Microsoft.Insights/dataCollectionEndpoints@2023-03-11' = {
  name: dbsClientConsoleApplogsEndpointName
  location: location
  tags: tags
  properties: {}
}

resource dbsClientConsoleAppLogsRule 'Microsoft.Insights/dataCollectionRules@2023-03-11' = {
  name: dbsClientConsoleAppLogsRuleName
  location: location
  kind: 'Windows'
  tags: tags
  properties: {
    dataCollectionEndpointId: dbsClientConsoleApplogsEndpoint.id
    streamDeclarations: {
      'Custom-Json-DbsClientConsoleApplogs_CL': {
        columns: [
          {
            name: 'TimeGenerated'
            type: 'datetime'
          }
          {
            name: 'Message'
            type: 'string'
          }
        ]
      }
    }
    dataSources: {
      logFiles: [
        {
          streams: [
            'Custom-Json-DbsClientConsoleApplogs_CL'
          ]
          filePatterns: [
            'C:\\Users\\SmokeTests\\*.log'
          ]
          format: 'json'
          name: 'DbsConsoleAppLog'
        }
      ]
    }
    destinations: {
      logAnalytics: [
        {
          name: logAnalyticsWorkspaceName
          workspaceResourceId: logAnalyticsWorkspace.id
        }
      ]
    }
    dataFlows: [
      {
        streams: [
          'Custom-Json-DbsClientConsoleApplogs_CL'
        ]
        destinations: [
          logAnalyticsWorkspaceName
        ]
        transformKql: 'source'
        outputStream: 'Custom-DbsClientConsoleApplogs_CL'
      }
    ]
  }
}

output vmName string = vm.name
output clientVirtualNetworkName string = vnet.name
output firewallName string = firewall.name
output routeTableName string = routeTable.name
