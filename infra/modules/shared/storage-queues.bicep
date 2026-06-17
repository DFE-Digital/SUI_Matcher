@description('The name of the storage account to configure')
param storageAccountName string

@description('Storage queue names to create')
param queueNames array

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' existing = {
  name: '${storageAccountName}/default'
}

resource queues 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = [
  for queueName in queueNames: {
    parent: queueService
    name: queueName
  }
]
