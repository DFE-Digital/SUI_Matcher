@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('The address prefix for the virtual network')
param containerAppVnet string = '192.168.0.0/25'

@description('Container App environment subnet')
param containerAppEnvSubnet string = '192.168.0.0/26'

@description('Container App environment subnet')
param containerAppFirewallSubnet string = '192.168.0.64/26'

@description('environmentName')
param environmentName string

@description('environmentPrefix')
param environmentPrefix string

@description('Tags that will be applied to all resources')
param tags object = {}

var lowercaseEnvironmentName = toLower(environmentName)

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-mi-01'
  location: location
  tags: tags
}

// The below resource can only contain alpha numeric characters - facepalm!
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: '${environmentPrefix}${lowercaseEnvironmentName}acr01'
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}


// resource caeMiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
//   name: guid(containerRegistry.id, managedIdentity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
//   scope: containerRegistry
//   properties: {
//     principalId: managedIdentity.properties.principalId
//     principalType: 'ServicePrincipal'
//     roleDefinitionId:  subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
//   }
// }
// Arc pull to be added to MI after deploy AcrPull
// Cannot add role assignments due to policies

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-loganalytics-01'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

param dashboard_name string = 'sui-pilot-dashboard'
param log_analytics_workspace_id string = '${environmentPrefix}-${toLower(environmentName)}-loganalytics-01'
param log_analytics_workspace_external_id string = '/subscriptions/${subscription().id}/resourceGroups/${resourceGroup().id}/providers/microsoft.operationalinsights/workspaces/${log_analytics_workspace_id}'

resource suiPilotDashboard 'Microsoft.Portal/dashboards@2022-12-01-preview' = {
  name: dashboard_name
  location: location
  tags: {
    'hidden-title': 'SUI Pilot Dashboard'
    Environment: lowercaseEnvironmentName
    Product: 'SUI'
    'Service Offering': 'SUI'
  }
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          {
            position: {
              x: 0
              y: 0
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                }
                {
                  name: 'Scope'
                  value: {
                    resourceIds: [
                      log_analytics_workspace_external_id
                    ]
                  }
                  isOptional: true
                }
                {
                  name: 'Version'
                  value: '2.0'
                  isOptional: true
                }
                {
                  name: 'TimeRange'
                  value: 'P7D'
                  isOptional: true
                }
                {
                  name: 'DashboardId'
                  isOptional: true
                }
                {
                  name: 'DraftRequestParameters'
                  isOptional: true
                }
                {
                  name: 'Query'
                  value: 'ContainerAppConsoleLogs_CL\n| where \n    Log_s has "resulted in match status \'Match\'" or \n    Log_s has "resulted in match status \'PotentialMatch\'" or \n    Log_s has "resulted in match status \'ManyMatch\'" or \n    Log_s has "resulted in match status \'NoMatch\'" or \n    Log_s has "returning match status \'Error\'"\n| extend MatchStatus = case(\n                           Log_s has "resulted in match status \'Match\'",\n                           "Match",\n                           Log_s has "resulted in match status \'PotentialMatch\'",\n                           "PotentialMatch",\n                           Log_s has "resulted in match status \'ManyMatch\'",\n                           "ManyMatch",    \n                           Log_s has "resulted in match status \'NoMatch\'",\n                           "NoMatch",    \n                           Log_s has "returning match status \'Error\'",\n                           "Error",\n                           "Other"\n                       )\n| summarize Count = count() by MatchStatus\n| render piechart\n\n'
                  isOptional: true
                }
                {
                  name: 'ControlType'
                  value: 'FrameControlChart'
                  isOptional: true
                }
                {
                  name: 'SpecificChart'
                  value: 'Pie'
                  isOptional: true
                }
                {
                  name: 'PartTitle'
                  value: 'Analytics'
                  isOptional: true
                }
                {
                  name: 'PartSubTitle'
                  value: log_analytics_workspace_id
                  isOptional: true
                }
                {
                  name: 'Dimensions'
                  value: {
                    xAxis: {
                      name: 'MatchStatus'
                      type: 'string'
                    }
                    yAxis: [
                      {
                        name: 'Count'
                        type: 'long'
                      }
                    ]
                    splitBy: []
                    aggregation: 'Sum'
                  }
                  isOptional: true
                }
                {
                  name: 'LegendOptions'
                  value: {
                    isEnabled: true
                    position: 'Bottom'
                  }
                  isOptional: true
                }
                {
                  name: 'IsQueryContainTimeRange'
                  value: false
                  isOptional: true
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
              settings: {
                content: {}
              }
            }
          }
          {
            position: {
              x: 0
              y: 4
              rowSpan: 4
              colSpan: 6
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                }
                {
                  name: 'Scope'
                  value: {
                    resourceIds: [
                      log_analytics_workspace_external_id
                    ]
                  }
                  isOptional: true
                }
                {
                  name: 'Version'
                  value: '2.0'
                  isOptional: true
                }
                {
                  name: 'TimeRange'
                  value: 'P7D'
                  isOptional: true
                }
                {
                  name: 'DashboardId'
                  isOptional: true
                }
                {
                  name: 'DraftRequestParameters'
                  isOptional: true
                }
                {
                  name: 'Query'
                  value: 'ContainerAppConsoleLogs_CL\n| where \n    Log_s has "resulted in match status \'Match\'" or \n    Log_s has "resulted in match status \'PotentialMatch\'" or \n    Log_s has "resulted in match status \'ManyMatch\'" or \n    Log_s has "resulted in match status \'NoMatch\'" or \n    Log_s has "returning match status \'Error\'"\n| extend MatchStatus = case(\n                           Log_s has "resulted in match status \'Match\'",\n                           "Match",\n                           Log_s has "resulted in match status \'PotentialMatch\'",\n                           "PotentialMatch",\n                           Log_s has "resulted in match status \'ManyMatch\'",\n                           "ManyMatch",    \n                           Log_s has "resulted in match status \'NoMatch\'",\n                           "NoMatch",    \n                           Log_s has "returning match status \'Error\'",\n                           "Error",\n                           "Other"\n                       )\n| summarize Count = count() by MatchStatus\n| render piechart\n\n'
                  isOptional: true
                }
                {
                  name: 'ControlType'
                  value: 'FrameControlChart'
                  isOptional: true
                }
                {
                  name: 'SpecificChart'
                  value: 'Pie'
                  isOptional: true
                }
                {
                  name: 'PartTitle'
                  value: 'Analytics'
                  isOptional: true
                }
                {
                  name: 'PartSubTitle'
                  value: log_analytics_workspace_id
                  isOptional: true
                }
                {
                  name: 'Dimensions'
                  value: {
                    xAxis: {
                      name: 'MatchStatus'
                      type: 'string'
                    }
                    yAxis: [
                      {
                        name: 'Count'
                        type: 'long'
                      }
                    ]
                    splitBy: []
                    aggregation: 'Sum'
                  }
                  isOptional: true
                }
                {
                  name: 'LegendOptions'
                  value: {
                    isEnabled: true
                    position: 'Bottom'
                  }
                  isOptional: true
                }
                {
                  name: 'IsQueryContainTimeRange'
                  value: false
                  isOptional: true
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
              settings: {
                content: {}
              }
            }
          }
          {
            position: {
              x: 6
              y: 4
              rowSpan: 4
              colSpan: 7
            }
            metadata: {
              inputs: [
                {
                  name: 'resourceTypeMode'
                  isOptional: true
                }
                {
                  name: 'ComponentId'
                  isOptional: true
                }
                {
                  name: 'Scope'
                  value: {
                    resourceIds: [
                      log_analytics_workspace_external_id
                    ]
                  }
                  isOptional: true
                }
                {
                  name: 'Version'
                  value: '2.0'
                  isOptional: true
                }
                {
                  name: 'TimeRange'
                  value: 'P7D'
                  isOptional: true
                }
                {
                  name: 'DashboardId'
                  isOptional: true
                }
                {
                  name: 'DraftRequestParameters'
                  isOptional: true
                }
                {
                  name: 'Query'
                  value: 'ContainerAppConsoleLogs_CL\n| where \n    Log_s has "resulted in match status \'Match\'" or \n    Log_s has "resulted in match status \'PotentialMatch\'" or \n    Log_s has "resulted in match status \'ManyMatch\'" or \n    Log_s has "resulted in match status \'NoMatch\'" or \n    Log_s has "returning match status \'Error\'"\n| extend MatchStatus = case(\n                           Log_s has "resulted in match status \'Match\'",\n                           "Match",\n                           Log_s has "resulted in match status \'PotentialMatch\'",\n                           "PotentialMatch",\n                           Log_s has "resulted in match status \'ManyMatch\'",\n                           "ManyMatch",    \n                           Log_s has "resulted in match status \'NoMatch\'",\n                           "NoMatch",    \n                           Log_s has "returning match status \'Error\'",\n                           "Error",\n                           "Other"\n                       )\n| summarize Count = count() by MatchStatus\n| render piechart\n\n'
                  isOptional: true
                }
                {
                  name: 'ControlType'
                  value: 'FrameControlChart'
                  isOptional: true
                }
                {
                  name: 'SpecificChart'
                  value: 'Pie'
                  isOptional: true
                }
                {
                  name: 'PartTitle'
                  value: 'Analytics'
                  isOptional: true
                }
                {
                  name: 'PartSubTitle'
                  value: log_analytics_workspace_id
                  isOptional: true
                }
                {
                  name: 'Dimensions'
                  value: {
                    xAxis: {
                      name: 'MatchStatus'
                      type: 'string'
                    }
                    yAxis: [
                      {
                        name: 'Count'
                        type: 'long'
                      }
                    ]
                    splitBy: []
                    aggregation: 'Sum'
                  }
                  isOptional: true
                }
                {
                  name: 'LegendOptions'
                  value: {
                    isEnabled: true
                    position: 'Bottom'
                  }
                  isOptional: true
                }
                {
                  name: 'IsQueryContainTimeRange'
                  value: false
                  isOptional: true
                }
              ]
              type: 'Extension/Microsoft_OperationsManagementSuite_Workspace/PartType/LogsDashboardPart'
              settings: {
                content: {}
              }
            }
          }
        ]
      }
    ]
    metadata: {
      model: {
        timeRange: {
          value: {
            relative: {
              duration: 24
              timeUnit: 1
            }
          }
          type: 'MsPortalFx.Composition.Configuration.ValueTypes.TimeRange'
        }
        filterLocale: {
          value: 'en-us'
        }
        filters: {
          value: {
            MsPortalFx_TimeRange: {
              model: {
                format: 'utc'
                granularity: 'auto'
                relative: '30d'
              }
            }
          }
        }
      }
    }
  }
}

resource caevnets 'Microsoft.Network/virtualNetworks@2022-07-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-vnet-cae-01'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        containerAppVnet
      ]
    }
    subnets: [
      {
        name: '${environmentPrefix}-${lowercaseEnvironmentName}-subnet-cae-01'
        properties: {
          addressPrefix: containerAppEnvSubnet
        }
      }
      {
        name: '${environmentPrefix}-${lowercaseEnvironmentName}-vnet-fw-01'
        properties: {
          addressPrefix: containerAppFirewallSubnet
        }
      }
    ]
  }
}


resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-cae-01'
  location: location
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

  }
  tags: tags

  resource aspireDashboard 'dotNetComponents' = {
    name: '${environmentPrefix}-${lowercaseEnvironmentName}-dashboard-01'
    properties: {
      componentType: 'AspireDashboard'
    }
  }
}

// Another which doesn't like dashes in the name
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${environmentPrefix}${lowercaseEnvironmentName}sa01'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
  tags: tags
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource blobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-container-01'
  parent: blobServices
  properties: {
    publicAccess: 'None'
  }
}


resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-appinsights-01'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
  tags: tags
}

param dataCollectionRules_DbsClientConsoleAppLogsRule_name string = 'DbsClientConsoleAppLogsRule'

// resource dataCollectionRules_DbsClientConsoleAppLogsRule_name_resource 'Microsoft.Insights/dataCollectionRules@2023-03-11' = {
//   name: dataCollectionRules_DbsClientConsoleAppLogsRule_name
//   location: location
//   tags: {
//     Environment: lowercaseEnvironmentName
//     Product: 'SUI'
//     'Service Offering': 'SUI'
//   }
//   kind: 'Windows'
//   properties: {
//     streamDeclarations: {
//       'Custom-DbsClientConsoleAppLogs_CL': {
//         columns: [
//           {
//             name: 'TimeGenerated'
//             type: 'datetime'
//           }
//           {
//             name: 'Message'
//             type: 'string'
//           }
//         ]
//       }
//     }
//     dataSources: {
//       logFiles: [
//         {
//           streams: [
//             'Custom-DbsClientConsoleAppLogs_CL'
//           ]
//           filePatterns: [
//             'C:\\Users\\AzCopy\\${environmentPrefix}-${lowercaseEnvironmentName}-container-01\\*.log'
//           ]
//           format: 'json'
//           name: 'Custom-DbsClientConsoleAppLog'
//         }
//       ]
//     }
//     destinations: {
//       logAnalytics: [
//         {
//           workspaceResourceId: logAnalyticsWorkspace.id
//           name: 'la-479495940'
//         }
//       ]
//     }
//     dataFlows: [
//       {
//         streams: [
//           'Custom-DbsClientConsoleAppLogs_CL'
//         ]
//         destinations: [
//           'la-479495940'
//         ]
//         transformKql: 'source'
//         outputStream: 'Custom-DbsClientConsoleAppLogs_CL'
//       }
//     ]
//   }
// }


output MANAGED_IDENTITY_CLIENT_ID string = managedIdentity.properties.clientId
output MANAGED_IDENTITY_NAME string = managedIdentity.name
output MANAGED_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.name
output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.properties.loginServer
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = managedIdentity.id
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = containerAppEnvironment.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppEnvironment.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = containerAppEnvironment.properties.defaultDomain
output APPLICATION_INSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString

