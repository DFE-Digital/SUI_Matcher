@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentPrefix string

// 3 - 24 alphanumeric characters 
resource secrets 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${environmentPrefix}-int-kv01'
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'secrets'
  }
}

output vaultUri string = secrets.properties.vaultUri
