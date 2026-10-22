@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentName string

param environmentPrefix string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('Tags that will be applied to the Key Vault')
param tags object = {}

@allowed([
  'Enabled'
  'Disabled'
])
@description('Whether the Key Vault accepts public network traffic. Set to Disabled only when the caller provisions a Key Vault private endpoint with private DNS.')
param publicNetworkAccess string = 'Enabled'

var environmentToken = toLower(substring(environmentName, 0, environmentName == 'Production' ? 4 : 3))
var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var uniqueSuffix = take(
  uniqueString(subscription().subscriptionId, resourceGroup().name, environmentPrefix, environmentName, stackNameSuffix),
  3
)
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
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: publicNetworkAccess == 'Disabled' ? 'Deny' : 'Allow'
    }
  }
}

output name string = secrets.name
output vaultUri string = secrets.properties.vaultUri
