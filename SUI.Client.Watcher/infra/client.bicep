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
param Network string

@description('Subnet Range')
param subnetRange string

param location string = resourceGroup().location

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
  properties: {
    hardwareProfile: {
      vmSize: 'Standard_DS1_v2'
    }
    osProfile: {
      computerName: '${environmentPrefix}-${environmentName}-vm-01'
      adminUsername: adminUsername
      adminPassword: adminPassword
    }
    storageProfile: {
      imageReference: {
        publisher: 'MicrosoftWindowsServer'
        offer: 'WindowsServer'
        sku: '2019-Datacenter'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
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
