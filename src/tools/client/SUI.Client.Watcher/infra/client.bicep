@description('environmentName')
param environmentName string = 'integration'

@description('Username for the Virtual Machine.')
param adminUsername string

@description('Password for the Virtual Machine.')
@minLength(12)
@secure()
param adminPassword string

@description('environmentPrefix')
param environmentPrefix string = 's215d01'

@description('Network')
param Network string = '192.168.0.128/25'

@description('Subnet Range')
param subnetRange string = '192.168.0.128/26'

param location string = resourceGroup().location

@secure()
param extensions_Microsoft_Insights_VMDiagnosticsSettings_xmlCfg string

@secure()
param storageAccountName string

@secure()
param storageAccountKey string

@secure()
param storageAccountEndPoint string

@description('Tags for the resources')
param paramTags object = {
  Product: 'SUI'
  Environment: 'Dev'
  'Service Offering': 'SUI'
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-02-01' = {
  name: '${environmentPrefix}-${environmentName}-clientvnet-01'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        Network
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

resource vm_diagnostics_settings 'Microsoft.Compute/virtualMachines/extensions@2024-07-01' = {
  parent: vm
  name: 'Microsoft.Insights.VMDiagnosticsSettings'
  location: 'westeurope'
  tags: paramTags
  properties: {
    autoUpgradeMinorVersion: true
    publisher: 'Microsoft.Azure.Diagnostics'
    type: 'IaaSDiagnostics'
    typeHandlerVersion: '1.5'
    settings: {
      StorageAccount: '${environmentPrefix}${storageAccountName}'
      xmlCfg: extensions_Microsoft_Insights_VMDiagnosticsSettings_xmlCfg
    }
    protectedSettings: {
      storageAccountName: '${environmentPrefix}${storageAccountName}'
      storageAccountKey: storageAccountKey
      storageAccountEndPoint: storageAccountEndPoint
    }
  }
}

param virtualNetworks_vnetfw_name string = '${environmentPrefix}-vnetfw-01'

// Hardcoded external ID for the integration VNet
param virtualNetworks_integration_vnet_cae_01_externalid string = '/subscriptions/8fc7ea96-7305-492f-85f7-09069bb8fd29/resourceGroups/${environmentPrefix}-${toLower(environmentName)}/providers/Microsoft.Network/virtualNetworks/${environmentPrefix}-${toLower(environmentName)}-vnet-cae-01'

resource virtualNetworks_vnetfw_name_resource 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: virtualNetworks_vnetfw_name
  location: location
  tags: paramTags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/23'
      ]
    }
    encryption: {
      enabled: false
      enforcement: 'AllowUnencrypted'
    }
    privateEndpointVNetPolicies: 'Disabled'
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefixes: [
            '10.0.0.0/24'
          ]
          delegations: []
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
      {
        name: 'AzureFirewallSubnet'
        properties: {
          addressPrefixes: [
            '10.0.1.0/26'
          ]
          delegations: []
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
    ]
    virtualNetworkPeerings: [
          {
            name: 'peer-containers'
            properties: {
              peeringState: 'Connected'
              peeringSyncLevel: 'FullyInSync'
              remoteVirtualNetwork: {
                id: virtualNetworks_integration_vnet_cae_01_externalid
              }
              allowVirtualNetworkAccess: true
              allowForwardedTraffic: false
              allowGatewayTransit: false
              useRemoteGateways: false
              doNotVerifyRemoteGateways: false
              peerCompleteVnets: true
              remoteAddressSpace: {
                addressPrefixes: [
                  '192.168.0.0/25'
                ]
              }
              remoteVirtualNetworkAddressSpace: {
                addressPrefixes: [
                  '192.168.0.0/25'
                ]
              }
            }
            type: 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings'
          }
        ]
    enableDdosProtection: false
  }
}

resource virtualNetworks_vnetfw_name_AzureFirewallSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: virtualNetworks_vnetfw_name_resource
  name: 'AzureFirewallSubnet'
  properties: {
    addressPrefixes: [
      '10.0.1.0/26'
    ]
    delegations: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
  }
}

resource virtualNetworks_vnetfw_name_default 'Microsoft.Network/virtualNetworks/subnets@2024-05-01' = {
  parent: virtualNetworks_vnetfw_name_resource
  name: 'default'
  properties: {
    addressPrefixes: [
      '10.0.0.0/24'
    ]
    delegations: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
  }
}



param azureFirewalls_vnetfw_Firewall_name string = '${environmentPrefix}-vnetfw-Firewall'

resource publicIP 'Microsoft.Network/publicIPAddresses@2023-06-01' = {
  name: '${environmentPrefix}-${environmentName}-pib-01'
  location: location
  tags: paramTags
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource firewallPolicy 'Microsoft.Network/firewallPolicies@2022-01-01'= {
  name: '${environmentPrefix}-${environmentName}-fwp-01'
  location: location
  properties: {
    threatIntelMode: 'Alert'
  }
  tags: paramTags
}

resource azureFirewalls_vnetfw_Firewall_name_resource 'Microsoft.Network/azureFirewalls@2024-05-01' = {
  name: azureFirewalls_vnetfw_Firewall_name
  location: location
  tags: paramTags
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
            id: '${virtualNetworks_vnetfw_name_resource.id}/subnets/AzureFirewallSubnet'
          }
        }
      }
    ]
    networkRuleCollections: []
    applicationRuleCollections: []
    natRuleCollections: []
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
            sourceIpGroups: []
          }
        ]
      }
    ]
  }
}

param routeTables_integration_rt_01_name string = '${environmentPrefix}-${toLower(environmentName)}-rt-01'

resource routeTables_integration_rt_01_name_resource 'Microsoft.Network/routeTables@2024-05-01' = {
  name: routeTables_integration_rt_01_name
  location: location
  tags: paramTags
  properties: {
    disableBgpRoutePropagation: false
    routes: [
      {
        name: 'DefaultToFirewall'
        properties: {
          addressPrefix: '0.0.0.0/0'
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: azureFirewalls_vnetfw_Firewall_name_resource.properties.ipConfigurations[0].properties.privateIPAddress
        }
        type: 'Microsoft.Network/routeTables/routes'
      }
    ]
  }
}


// --- LEGACY FOR REFERENCE --- 

// resource publicIP 'Microsoft.Network/publicIPAddresses@2023-06-01' = {
//   name: '${environmentPrefix}-${environmentName}-pib-01'
//   location: location
//   tags: tags
//   sku: {
//     name: 'Standard'
//   }
//   properties: {
//     publicIPAllocationMethod: 'static'
//     publicIPAddressVersion: 'IPv4'
//   }
// }
// Cannot create IPs due to policies

// resource firewallPolicy 'Microsoft.Network/firewallPolicies@2022-01-01'= {
//   name: '${environmentPrefix}-${environmentName}-fwp-01'
//   location: location
//   properties: {
//     threatIntelMode: 'Alert'
//   }
//   tags: tags
// }

// resource applicationRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2022-01-01' = {
//   parent: firewallPolicy
//   name: 'DefaultApplicationRuleCollectionGroup'
//   properties: {
//     priority: 300
//     ruleCollections: [
//       {
//         ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
//         action: {
//           type: 'Allow'
//         }
//         name: 'Global-rules-arc'
//         priority: 300
//         rules: [
//           {
//             ruleType: 'ApplicationRule'
//             name: 'global-rule-01'
//             protocols: [
//               {
//                 protocolType: 'Https'
//                 port: 443
//               }
//             ]
//             targetFqdns: [
//               'int.api.service.nhs.uk'
//             ]
//             terminateTLS: false
//             sourceIpGroups: [
//               infraIpGroup.id
//             ]
//           }
//         ]
//       }
//     ]
//   }
// }

// resource fireWall 'Microsoft.Network/azureFirewalls@2021-03-01' = {
//   name: '${environmentPrefix}-${environmentName}-fw-01'
//   location: location
//   dependsOn: [
//     containerAppEnvironment
//     applicationRuleCollectionGroup
//   ]
//   properties: {
//     ipConfigurations: [{
//       name: 'fw-ip-config-01'
//       properties: {
//         publicIPAddress: {
//           id: publicIP.id
//         }
//         subnet: {
//           id: caevnets.properties.subnets[1].id
//         }
//       }
//     }]
//     firewallPolicy: {
//       id: firewallPolicy.id
//     }
//   }
//   tags: tags
// }

// resource routeTable 'Microsoft.Network/routeTables@2024-05-01' = {
//   location: location
//   name: '${environmentPrefix}-${environmentName}-rt-01'
//   properties: {
//     disableBgpRoutePropagation: true
//     routes: [
//       {
//         name: '${environmentPrefix}-${environmentName}-internet'
//         properties: {
//           addressPrefix: '0.0.0.0/0'
//           nextHopIpAddress: fireWall.properties.ipConfigurations[0].properties.privateIPAddress
//           nextHopType: 'Internet'
//         }
//         type: 'string'
//       }
//     ]
//   }
//   tags: tags
// }


