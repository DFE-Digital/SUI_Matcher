@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentName string

param environmentPrefix string

// 3 - 24 alphanumeric characters 
resource secrets 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${environmentPrefix}-${toLower(substring(environmentName, 0, environmentName == 'Production' ? 4 : 3))}-kv01'
  location: location
  tags: {
    'aspire-resource-name': 'secrets'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
}

output vaultUri string = secrets.properties.vaultUri
