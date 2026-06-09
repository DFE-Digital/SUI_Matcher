@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('Tags that will be applied to all resources')
param tags object = {}

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-loganalytics-01'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource loganalyticsDbsConsoleApplogs 'Microsoft.OperationalInsights/workspaces/tables@2025-02-01' = {
  parent: logAnalyticsWorkspace
  name: 'DbsClientConsoleApplogs_CL'
  properties: {
    totalRetentionInDays: 30
    plan: 'Analytics'
    schema: {
      name: 'DbsClientConsoleApplogs_CL'
      columns: [
        {
          name: 'Message'
          type: 'string'
        }
        {
          name: 'TimeGenerated'
          type: 'datetime'
        }
      ]
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-appinsights-01'
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

var applicationInsightsIngestionEndpoint = split(split(applicationInsights.properties.ConnectionString, 'IngestionEndpoint=')[1], ';')[0]
var applicationInsightsIngestionHost = split(replace(replace(applicationInsightsIngestionEndpoint, 'https://', ''), 'http://', ''), '/')[0]

output workspaceName string = logAnalyticsWorkspace.name
output workspaceId string = logAnalyticsWorkspace.id
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output applicationInsightsIngestionHost string = applicationInsightsIngestionHost
