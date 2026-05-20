@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param environmentName string

param environmentPrefix string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

var environmentToken = toLower(substring(environmentName, 0, environmentName == 'Production' ? 4 : 3))
var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var uniqueSuffix = take(uniqueString(subscription().subscriptionId, resourceGroup().name, environmentPrefix, environmentName, stackNameSuffix), 3)

@secure()
@description('NHS Digital client ID secret value.')
param nhsDigitalClientId string = ''

@secure()
@description('NHS Digital key ID secret value.')
param nhsDigitalKid string = ''

@secure()
@description('NHS Digital private key secret value.')
param nhsDigitalPrivateKey string = ''

// 3 - 24 alphanumeric characters
resource secrets 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${environmentPrefix}-${environmentToken}${stackNameToken}-kv${uniqueSuffix}'
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

resource nhsDigitalClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(nhsDigitalClientId)) {
  parent: secrets
  name: 'nhs-digital-client-id'
  properties: {
    value: nhsDigitalClientId
  }
}

resource nhsDigitalKidSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(nhsDigitalKid)) {
  parent: secrets
  name: 'nhs-digital-kid'
  properties: {
    value: nhsDigitalKid
  }
}

resource nhsDigitalPrivateKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(nhsDigitalPrivateKey)) {
  parent: secrets
  name: 'nhs-digital-private-key'
  properties: {
    value: nhsDigitalPrivateKey
  }
}

output name string = secrets.name
output vaultUri string = secrets.properties.vaultUri
