@description('The location used for all deployed resources')
param location string

@description('The resource ID of the Container Apps managed environment')
param containerAppsEnvironmentId string

@description('The login server for the container registry')
param containerRegistryServer string

@description('The container registry resource name')
param containerRegistryName string

@description('The resource ID of the managed identity')
param managedIdentityId string

@description('The principal ID of the managed identity, used for RBAC assignments')
param managedIdentityPrincipalId string

@description('The client ID of the managed identity')
param managedIdentityClientId string

@description('Name of the deployment environment used for ASP.NET runtime configuration')
param environmentName string

@description('The container image tag to deploy')
param imageTag string

@secure()
@description('The Key Vault URI used by the application secrets connection string')
param keyVaultUri string

@secure()
@description('The Application Insights connection string')
param applicationInsightsConnectionString string

@description('Tags that will be applied to all resources')
param tags object = {}

var appName = 'external-api'
var targetPort = 8080

module acrPullRbac '../shared/acr-pull-rbac.bicep' = {
  name: '${appName}-acr-pull-rbac'
  params: {
    containerRegistryName: containerRegistryName
    principalId: managedIdentityPrincipalId
  }
}

resource externalApi 'Microsoft.App/containerApps@2024-10-02-preview' = {
  name: appName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironmentId
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
        allowInsecure: false
      }
      registries: [
        {
          server: containerRegistryServer
          identity: managedIdentityId
        }
      ]
      secrets: [
        {
          name: 'connectionstrings--secrets'
          value: keyVaultUri
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
          name: appName
          image: '${containerRegistryServer}/${appName}:${imageTag}'
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
              value: environmentName
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
  dependsOn: [
    acrPullRbac
  ]
}

output name string = externalApi.name
output id string = externalApi.id
