@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('The resource ID of the storage account that emits blob events and hosts the destination queue')
param storageAccountId string

@description('The storage queue that receives Event Grid messages')
param queueName string = 'storage-process-job'

@description('The blob container that emits file-created events for processing')
param incomingContainerName string = 'incoming'

@description('Tags that will be applied to all resources')
param tags object = {}

// bepeg = blob event processor event grid
var systemTopicName = toLower('${take(environmentPrefix, 8)}${take(lowercaseEnvironmentName, 8)}bepeg${take(uniqueString(resourceGroup().id, environmentPrefix, lowercaseEnvironmentName), 5)}')
var eventSubscriptionName = 'incoming-blob-created-to-storage-process-job'

resource systemTopic 'Microsoft.EventGrid/systemTopics@2025-02-15' = {
  name: systemTopicName
  location: location
  tags: tags
  properties: {
    source: storageAccountId
    topicType: 'Microsoft.Storage.StorageAccounts'
  }
}

resource eventSubscription 'Microsoft.EventGrid/systemTopics/eventSubscriptions@2025-02-15' = {
  parent: systemTopic
  name: eventSubscriptionName
  properties: {
    destination: {
      endpointType: 'StorageQueue'
      properties: {
        resourceId: storageAccountId
        queueName: queueName
      }
    }
    eventDeliverySchema: 'EventGridSchema'
    filter: {
      includedEventTypes: [
        'Microsoft.Storage.BlobCreated'
      ]
      isSubjectCaseSensitive: true
      subjectBeginsWith: '/blobServices/default/containers/${incomingContainerName}/blobs/'
    }
  }
}

output systemTopicName string = systemTopic.name
output systemTopicId string = systemTopic.id
output eventSubscriptionName string = eventSubscription.name
output eventSubscriptionId string = eventSubscription.id
