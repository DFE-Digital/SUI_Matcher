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

@description('The principal ID of the managed identity, used for RBAC assignments')
param managedIdentityPrincipalId string

@description('The client ID of the managed identity')
param managedIdentityClientId string

@description('The storage account name')
param storageAccountName string

@description('The queue name to trigger on')
param queueName string = 'storage-process-job'

@description('The blob service URI of the storage account')
param blobServiceUri string

@description('The queue service URI of the storage account')
param queueServiceUri string

@description('The container registry server endpoint')
param containerRegistryServer string

@description('The container image name')
param imageName string = 'sui-client-storage-process-job'

@description('The container image tag to deploy')
param imageTag string

@description('The base address of the matching API')
param matchApiBaseAddress string

@description('Tags that will be applied to all resources')
param tags object = {}

@description('Whether or not to include role assignments, since some environments may restrict these.')
param includeRoleAssignments bool = true

// https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
// 32 chars max
var jobName = toLower('${take(environmentPrefix, 8)}-${take(lowercaseEnvironmentName, 3)}-storage-process-job')
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource storageBlobDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (includeRoleAssignments) {
  scope: storageAccount
  name: guid(storageAccount.id, managedIdentityPrincipalId, storageBlobDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource storageQueueDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (includeRoleAssignments) {
  scope: storageAccount
  name: guid(storageAccount.id, managedIdentityPrincipalId, storageQueueDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource storageProcessJob 'Microsoft.App/jobs@2024-10-02-preview' = {
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
      triggerType: 'Event'
      replicaTimeout: 21600 // 6 hours, to allow for long-running processing of large files
      replicaRetryLimit: 0 // We handle errors in code and don't want automatic retries which could lead to multiple replicas running concurrently and processing the same queue message
      registries: [
        {
          server: containerRegistryServer
          identity: managedIdentityId
        }
      ]
      eventTriggerConfig: {
        replicaCompletionCount: 1
        parallelism: 1
        scale: {
          minExecutions: 0
          maxExecutions: 1
          pollingInterval: 30
          rules: [
            {
              name: 'azure-queue-rule'
              type: 'azure-queue'
              metadata: {
                accountName: storageAccountName
                queueName: queueName
                queueLength: '1'
              }
              identity: managedIdentityId
            }
          ]
        }
      }
    }
    template: {
      containers: [
        {
          name: 'storage-process-job'
          image: '${containerRegistryServer}/${imageName}:${imageTag}'
          resources: {
            cpu: json('2.0')
            memory: '4Gi'
          }
          env: [
            {
              name: 'StorageProcessJob__BlobServiceUri'
              value: blobServiceUri
            }
            {
              name: 'StorageProcessJob__QueueServiceUri'
              value: queueServiceUri
            }
            {
              name: 'StorageProcessJob__MatchApiBaseAddress'
              value: matchApiBaseAddress
            }
            {
              name: 'StorageProcessJob__MaxDequeueCount'
              value: '1'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
          ]
        }
      ]
    }
  }
  dependsOn: [
    storageBlobDataContributorAssignment
    storageQueueDataContributorAssignment
  ]
}

output jobName string = storageProcessJob.name
output jobId string = storageProcessJob.id
