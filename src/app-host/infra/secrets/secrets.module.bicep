@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentName string

param environmentPrefix string

module secrets '../../../../infra/modules/shared/secrets.bicep' = {
  name: 'secrets'
  params: {
    location: location
    environmentName: environmentName
    environmentPrefix: environmentPrefix
  }
}

output vaultUri string = secrets.outputs.vaultUri
