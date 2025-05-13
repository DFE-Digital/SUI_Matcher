targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The prefix used for all deployed resources')
param environmentPrefix string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The email address to be used for monitoring alerts')
param monitoringActionGroupEmail string

@description('Turn on monitoring alerts')
param turnOnAlerts bool = true

var tags = {
  'azd-env-name': environmentName
  Product: 'SUI'
  Environment: environmentName
  EnvironmentPrefix: environmentPrefix
  'Service Offering': 'SUI'
}

module resources 'resources.bicep' = {
  name: 'resources'
  params: {
    location: location
    tags: tags
    environmentPrefix: environmentPrefix
    environmentName: environmentName
  }
}

module secrets 'secrets/secrets.module.bicep' = {
  name: 'secrets'
  params: {
    location: location
    environmentName: environmentName
    environmentPrefix: environmentPrefix
  }
}

module externalApi 'container-apps/containerapps.module.bicep' = {
  name: 'externalApi'
  params: {
    location: location
    azureContainerRegistryManagedIdentityId: resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
    azureContainerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    azureContainerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    secretsVaultUri: secrets.outputs.vaultUri
    applicationInsightsConnectionString: resources.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING
    managedIdentityClientId: resources.outputs.MANAGED_IDENTITY_CLIENT_ID
    azureEnvName: environmentName
    // Hard coded for now, until we can publish the image to the registry first
    imageName: 'external-api-integration'
    appName: 'external-api'
  }
}

module matchingApi 'container-apps/containerapps.module.bicep' = {
  name: 'matchingApi'
  params: {
    location: location
    azureContainerRegistryManagedIdentityId: resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
    azureContainerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    azureContainerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    secretsVaultUri: secrets.outputs.vaultUri
    applicationInsightsConnectionString: resources.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING
    managedIdentityClientId: resources.outputs.MANAGED_IDENTITY_CLIENT_ID
    azureEnvName: environmentName
    // Hard coded for now, until we can publish the image to the registry first
    imageName: 'matching-api-integration'
    appName: 'matching-api'
    extraEnvVars: [
      {
        name: 'services__external-api__http__0'
        value: 'http://external-api.internal.${resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}'
      }
      {
        name: 'services__external-api__https__0'
        value: 'https://external-api.internal.${resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}'
      }
    ]
  }
}

module yarp 'container-apps/containerapps.module.bicep' = {
  name: 'yarp'
  params: {
    location: location
    azureContainerRegistryManagedIdentityId: resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
    azureContainerAppsEnvironmentId: resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
    azureContainerRegistryEndpoint: resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
    secretsVaultUri: secrets.outputs.vaultUri
    applicationInsightsConnectionString: resources.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING
    managedIdentityClientId: resources.outputs.MANAGED_IDENTITY_CLIENT_ID
    azureEnvName: environmentName
    // Hard coded for now, until we can publish the image to the registry first
    imageName: 'yarp-integration'
    appName: 'yarp'
    extraEnvVars: [
      {
        name: 'services__matching-api__http__0'
        value: 'http://matching-api.internal.${resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}'
      }
      {
        name: 'services__matching-api__https__0'
        value: 'https://matching-api.internal.${resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN}'
      }
    ]
  }
}
    

module monitoring 'monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    turnOnAlerts: turnOnAlerts
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
    actionGroupEmail: monitoringActionGroupEmail
    
    // Implicit dependency on the container apps
    externalApiId: externalApi.outputs.id
    matchingApiId: matchingApi.outputs.id
    yarpId: yarp.outputs.id
  }
}

output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_REGISTRY_NAME string = resources.outputs.AZURE_CONTAINER_REGISTRY_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output APPLICATION_INSIGHTS_CONNECTION_STRING string = resources.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING
output SECRETS_VAULTURI string = secrets.outputs.vaultUri
