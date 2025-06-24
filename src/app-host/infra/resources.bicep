@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('environmentName')
param environmentName string

@description('environmentPrefix')
param environmentPrefix string

@description('container app managed environment number')
param containerAppManagedEnvironmentNumber string

@description('The address prefix for the virtual network')
param containerAppVnet string

@description('Container App environment subnet')
param containerAppEnvSubnet string

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

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-loganalytics-01'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource loganalyticsDbsConsoleApplogs 'Microsoft.OperationalInsights/workspaces/tables@2025-02-01' = {
  parent: logAnalyticsWorkspace
  name: 'DbsClientConsoleApplogs_CL'
  properties: {
    totalRetentionInDays: 30
    plan: 'Analytics'
    schema: {
      name: 'DbsClientConsoleApplogs_CL'
      columns: [
        {
          name: 'Message'
          type: 'string'
        }
        {
          name: 'TimeGenerated'
          type: 'datetime'
        }
      ]
    }
    retentionInDays: 30
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
          delegations: [
            {
              name: 'Microsoft.App.environments'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
    ]
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-cae-${containerAppManagedEnvironmentNumber}'
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
    publicNetworkAccess: 'Disabled' // For Pen Test destroy cae and rebuild with that set to Enabled
    vnetConfiguration: {
      infrastructureSubnetId: '${caevnets.id}/subnets/${environmentPrefix}-${lowercaseEnvironmentName}-subnet-cae-01'
      internal: true // For Pen Test destroy cae and rebuild with this value removed
    }
  }

  resource aspireDashboard 'dotNetComponents' = {
    name: '${environmentPrefix}-${lowercaseEnvironmentName}-dashboard-01'
    properties: {
      componentType: 'AspireDashboard'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-appinsights-01'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}


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
