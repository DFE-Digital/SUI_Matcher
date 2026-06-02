@description('The location used for all deployed resources')
param location string

@description('The resource ID of the Container Apps managed environment')
param containerAppsEnvironmentId string

@description('The default domain of the Container Apps managed environment')
param containerAppsEnvironmentDefaultDomain string

@description('The login server for the container registry')
param containerRegistryServer string

@description('The resource ID of the managed identity')
param managedIdentityId string

@description('The client ID of the managed identity')
param managedIdentityClientId string

@description('Name of the deployment environment used for ASP.NET runtime configuration')
param environmentName string

@description('The container image tag to deploy')
param imageTag string

@secure()
@description('The Application Insights connection string')
param applicationInsightsConnectionString string

@description('Tags that will be applied to all resources')
param tags object = {}

var appName = 'yarp'
var matchingApiName = 'matching-api'
var targetPort = 8080

resource yarp 'Microsoft.App/containerApps@2024-10-02-preview' = {
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
        external: true
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
              name: 'services__matching-api__http__0'
              value: 'http://${matchingApiName}.internal.${containerAppsEnvironmentDefaultDomain}'
            }
            {
              name: 'services__matching-api__https__0'
              value: 'https://${matchingApiName}.internal.${containerAppsEnvironmentDefaultDomain}'
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
}

output name string = yarp.name
output id string = yarp.id
