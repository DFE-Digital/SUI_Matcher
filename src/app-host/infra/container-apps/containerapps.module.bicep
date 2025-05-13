param location string
param azureContainerRegistryManagedIdentityId string
param azureContainerAppsEnvironmentId string
param azureContainerRegistryEndpoint string

@secure()
param secretsVaultUri string
param applicationInsightsConnectionString string
param managedIdentityClientId string
param azureEnvName string

param imageName string
param appName string
param targetPort int = 8080

@description('Additional environment variables for the container app')
param extraEnvVars array = []

resource containerApp 'Microsoft.App/containerApps@2024-02-02-preview' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${azureContainerRegistryManagedIdentityId}': {}
    }
  }
  properties: {
    environmentId: azureContainerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'single'
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
      ingress: {
        external: false
        targetPort: targetPort
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: azureContainerRegistryEndpoint
          identity: azureContainerRegistryManagedIdentityId
        }
      ]
      secrets: [
        {
          name: 'connectionstrings--secrets'
          value: secretsVaultUri
        }
        {
          name: 'app-insights-connection-string'
          value: applicationInsightsConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${azureContainerRegistryEndpoint}/app-host/${imageName}'
          name: appName
          env: [
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: string(targetPort)
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ConnectionStrings__secrets'
              secretRef: 'connectionstrings--secrets'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: azureEnvName
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
            ...extraEnvVars
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
    workloadProfileName: 'default' // Assuming default workload profile
  }
  tags: {
    'azd-service-name': appName
    'aspire-resource-name': appName
    Product: 'SUI'
    Environment: 'Integration'
    'Service Offering': 'SUI'
  }
}

output id string = containerApp.id