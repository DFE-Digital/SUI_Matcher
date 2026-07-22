@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('The resource ID of the Container Apps managed environment')
param containerAppsEnvironmentId string

@description('The resource ID of the managed identity')
param managedIdentityId string

@description('The client ID of the managed identity')
param managedIdentityClientId string

@description('The container registry server endpoint')
param containerRegistryServer string

@description('The container image name')
param imageName string = 'sui-client-graphql-process-job'

@description('The container image tag to deploy')
param imageTag string

@description('The base address of the matching API')
param matchApiBaseAddress string = ''

@secure()
@description('The Application Insights connection string')
param applicationInsightsConnectionString string

@secure()
@description('Runtime configuration values for the GraphQL process job.')
param graphqlProcessJobConfiguration object

@allowed([
  'automatic'
  'manual'
])
@description('The deployment mode for the job: automatic (scheduled) or manual (triggered on-demand)')
param deploymentMode string = 'manual'

@description('The cron expression for the scheduled trigger when deploymentMode is automatic')
param cronExpression string = '0 9,12,15 * * 1-5'

@description('Tags that will be applied to all resources')
param tags object = {}

// https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
// 32 chars max
var jobName = toLower('${take(environmentPrefix, 8)}-${take(lowercaseEnvironmentName, 3)}-graphql-process-job')

var graphqlProcessJobConfigurationEnvironment = [for setting in items(graphqlProcessJobConfiguration): {
  name: setting.key
  value: string(setting.value)
}]

var triggerType = deploymentMode == 'automatic' ? 'Schedule' : 'Manual'

resource graphqlProcessJob 'Microsoft.App/jobs@2024-10-02-preview' = {
  name: jobName
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
    workloadProfileName: 'default'
    configuration: {
      triggerType: triggerType
      replicaTimeout: 1800 // 30 minutes, ample time for GraphQL sync
      replicaRetryLimit: 0
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
      scheduleTriggerConfig: triggerType == 'Schedule' ? {
        cronExpression: cronExpression
        parallelism: 1
        replicaCompletionCount: 1
      } : null
      manualTriggerConfig: triggerType == 'Manual' ? {
        parallelism: 1
        replicaCompletionCount: 1
      } : null
    }
    template: {
      containers: [
        {
          name: 'graphql-process-job'
          image: '${containerRegistryServer}/${imageName}:${imageTag}'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: concat(
            [
              {
                name: 'GraphQLProcessJob__MatchApiBaseAddress'
                value: matchApiBaseAddress
              }
              {
                name: 'AZURE_CLIENT_ID'
                value: managedIdentityClientId
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
                name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
                secretRef: 'app-insights-connection-string'
              }
            ],
            graphqlProcessJobConfigurationEnvironment
          )
        }
      ]
    }
  }
}

output jobName string = graphqlProcessJob.name
output jobId string = graphqlProcessJob.id
