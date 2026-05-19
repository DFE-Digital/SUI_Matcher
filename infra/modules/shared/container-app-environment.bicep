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

@description('The address prefix for the virtual network')
param containerAppVnet string

@description('Container App environment subnet')
param containerAppEnvSubnet string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('The Log Analytics workspace name used by the container app environment')
param logAnalyticsWorkspaceName string

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource caeVnet 'Microsoft.Network/virtualNetworks@2022-07-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-vnet-cae-01'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        containerAppVnet
      ]
    }
    subnets: [
      {
        name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-subnet-cae-01'
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
      infrastructureSubnetId: '${caeVnet.id}/subnets/${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-subnet-cae-01'
      internal: true
    }
  }

  resource aspireDashboard 'dotNetComponents' = {
    name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-dashboard-01'
    properties: {
      componentType: 'AspireDashboard'
    }
  }
}

output name string = containerAppEnvironment.name
output id string = containerAppEnvironment.id
output defaultDomain string = containerAppEnvironment.properties.defaultDomain
output virtualNetworkName string = caeVnet.name
