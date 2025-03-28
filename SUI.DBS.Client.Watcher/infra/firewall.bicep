// param subnetName string
// param virtualNetworkName string
// param location string = resourceGroup().location

// resource vnet 'Microsoft.Network/virtualNetworks@2021-02-01' existing = {
//   name: virtualNetworkName
// }

// resource subnet 'Microsoft.Network/virtualNetworks/subnets@2021-02-01' = {
//   name: '${virtualNetworkName}/${subnetName}'
//   properties: {
//     addressPrefix: '10.0.1.0/24'
//   }
// }

// resource firewall 'Microsoft.Network/azureFirewalls@2021-02-01' = {
//   name: 'myFirewall'
//   location: location
//   properties: {
//     sku: {
//       name: 'AZFW_VNet'
//       tier: 'Standard'
//     }
//     ipConfigurations: [
//       {
//         name: 'azureFirewallIpConfiguration'
//         properties: {
//           subnet: {
//             id: subnet.id
//           }
//           publicIPAddress: {
//             id: '/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{publicIpAddressName}'
//           }
//         }
//       }
//     ]
//     firewallPolicy: {
//       id: firewallPolicy.id
//     }
//   }
// }

// resource firewallPolicy 'Microsoft.Network/firewallPolicies@2021-02-01' = {
//   name: 'myFirewallPolicy'
//   location: location
//   properties: {
//     ruleCollectionGroups: [
//       {
//         name: 'AllowIntApiService'
//         properties: {
//           priority: 100
//           ruleCollections: [
//             {
//               name: 'AllowIntApiServiceRuleCollection'
//               properties: {
//                 ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
//                 action: {
//                   type: 'Allow'
//                 }
//                 rules: [
//                   {
//                     name: 'AllowIntApiServiceRule'
//                     properties: {
//                       ruleType: 'ApplicationRule'
//                       sourceAddresses: [
//                         '*'
//                       ]
//                       protocols: [
//                         {
//                           protocolType: 'Https'
//                           port: 443
//                         }
//                       ]
//                       targetFqdns: [
//                         'int.api.service.nhs.uk'
//                       ]
//                     }
//                   }
//                 ]
//               }
//             }
//           ]
//         }
//       }
//     ]
//   }
// }
