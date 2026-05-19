@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentName string

param environmentPrefix string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

var environmentToken = toLower(substring(environmentName, 0, environmentName == 'Production' ? 4 : 3))
var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'

// 3 - 24 alphanumeric characters
resource secrets 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${environmentPrefix}-${environmentToken}${stackNameToken}-kv01'
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
    enableSoftDelete: true
    enablePurgeProtection: true
  }
}

output name string = secrets.name
output vaultUri string = secrets.properties.vaultUri
