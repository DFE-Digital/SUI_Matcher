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

@description('The name of the shared Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('The resource group that contains the shared Log Analytics workspace')
param logAnalyticsWorkspaceResourceGroupName string = resourceGroup().name

param dbsClientConsoleApplogsEndpointName string = 'DbsClientConsoleApplogsEndpoint'

param dbsClientConsoleAppLogsRuleName string = 'DbsClientConsoleAppLogsRule'

@description('Tags for the resources')
param tags object = {
  Product: 'SUI'
  Environment: 'Dev'
  'Service Offering': 'SUI'
}

var lowercaseEnvironmentName = toLower(environmentName)

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-06-01' existing = {
  scope: resourceGroup(logAnalyticsWorkspaceResourceGroupName)
  name: logAnalyticsWorkspaceName
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-02-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-ca-clientvnet-01'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        network
      ]
    }
    subnets: [
      {
        name: '${environmentPrefix}-${lowercaseEnvironmentName}-ca-clientsubnet-01'
        properties: {
          addressPrefix: subnetRange
        }
      }
    ]
  }
}

resource nic 'Microsoft.Network/networkInterfaces@2021-02-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-ca-nic-01'
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
  name: '${environmentPrefix}-${lowercaseEnvironmentName}-ca-vm-01'
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
