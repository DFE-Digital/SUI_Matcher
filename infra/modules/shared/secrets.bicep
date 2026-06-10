@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentName string

param environmentPrefix string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('Tags that will be applied to the Key Vault')
param tags object = {}

var environmentToken = toLower(substring(environmentName, 0, environmentName == 'Production' ? 4 : 3))
var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var uniqueSuffix = take(uniqueString(subscription().subscriptionId, resourceGroup().name, environmentPrefix, environmentName, stackNameSuffix), 3)
var keyVaultTags = union(tags, {
  'aspire-resource-name': 'secrets'
})

// 3 - 24 alphanumeric characters
resource secrets 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${environmentPrefix}-${environmentToken}${stackNameToken}-kv${uniqueSuffix}'
  location: location
  tags: keyVaultTags
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
