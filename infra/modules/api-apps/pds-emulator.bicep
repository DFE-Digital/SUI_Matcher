@description('The location used for all deployed resources')
param location string

@description('The resource ID of the Container Apps managed environment')
param containerAppsEnvironmentId string

@description('The login server for the container registry')
param containerRegistryServer string

@description('The resource ID of the managed identity')
param managedIdentityId string

@description('Name of the deployment environment used for ASP.NET runtime configuration')
param environmentName string

@description('The container image tag to deploy')
param imageTag string

@description('Tags that will be applied to all resources')
param tags object = {}

var appName = 'pds-emulator'
var targetPort = 8080

resource pdsEmulator 'Microsoft.App/containerApps@2024-10-02-preview' = {
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
    }
    template: {
      containers: [
        {
          name: appName
          image: '${containerRegistryServer}/${appName}:${imageTag}'
          env: [
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
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environmentName
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

output name string = pdsEmulator.name
output id string = pdsEmulator.id
