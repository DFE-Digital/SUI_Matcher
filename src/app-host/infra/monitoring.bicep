param actionGroupName string = 'On-Call Team'
// To handle the fact that bicep does not deploy the apps so the containers won't exist
// until after the first deployment. So this needs to be a phased approach to monitoring.
param turnOnAlerts bool = true
param location string = resourceGroup().location
param logAnalyticsWorkspaceId string
param actionGroupEmail string

param externalApiId string
param matchingApiId string
param yarpId string

var containerResources = [
  {
    name: 'external-api'
    id: externalApiId
  }
  {
    name: 'matching-api'
    id: matchingApiId
  }
  {
    name: 'yarp'
    id: yarpId
  }
]

resource supportTeamActionGroup 'Microsoft.Insights/actionGroups@2024-10-01-preview' = {
  name: actionGroupName
  location: 'global'
  properties: {
    enabled: true
    groupShortName: actionGroupName
    emailReceivers: [
      {
        name: actionGroupName
        emailAddress: actionGroupEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

resource CpuAlerts 'Microsoft.Insights/metricAlerts@2018-03-01' = [
  for container in containerResources: if (turnOnAlerts) {
    name: '${container.name}-cpu-alert'
    location: 'global'
    properties: {
      description: 'CPU usage alert for ${container}'
      severity: 2 // Medium severity
      enabled: true
      scopes: [
        container.id
      ]
      evaluationFrequency: 'PT1M'
      windowSize: 'PT5M'
      criteria: {
        allOf: [
          {
            name: 'HighCPU'
            metricName: 'CpuPercentage'
            operator: 'GreaterThan'
            threshold: 80
            timeAggregation: 'Average'
            criterionType: 'StaticThresholdCriterion'
          }
          {
            name: 'MediumCPU'
            metricName: 'CpuPercentage'
            operator: 'GreaterThan'
            threshold: 60
            timeAggregation: 'Average'
            criterionType: 'StaticThresholdCriterion'
          }
          {
            name: 'LowCPU'
            metricName: 'CpuPercentage'
            operator: 'GreaterThan'
            threshold: 40
            timeAggregation: 'Average'
            criterionType: 'StaticThresholdCriterion'
          }
        ]
        'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      }
      actions: [
        {
          actionGroupId: supportTeamActionGroup.id
        }
      ]
    }
  }
]

resource MemoryAlerts 'Microsoft.Insights/metricAlerts@2018-03-01' = [
  for container in containerResources: if (turnOnAlerts) {
    name: '${container.name}-memory-alert'
    location: 'global'
    properties: {
      description: 'Memory usage alert for ${container}'
      severity: 2 // Medium severity
      enabled: true
      scopes: [
        container.id
      ]
      evaluationFrequency: 'PT1M'
      windowSize: 'PT5M'
      criteria: {
        'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
        allOf: [
          {
            name: 'HighMemory'
            metricName: 'MemoryPercentage'
            operator: 'GreaterThan'
            threshold: 80
            timeAggregation: 'Average'
            criterionType: 'StaticThresholdCriterion'
          }
          {
            name: 'MediumMemory'
            metricName: 'MemoryPercentage'
            operator: 'GreaterThan'
            threshold: 60
            timeAggregation: 'Average'
            criterionType: 'StaticThresholdCriterion'
          }
          {
            name: 'LowMemory'
            metricName: 'MemoryPercentage'
            operator: 'GreaterThan'
            threshold: 40
            timeAggregation: 'Average'
            criterionType: 'StaticThresholdCriterion'
          }
        ]
      }
      actions: [
        {
          actionGroupId: supportTeamActionGroup.id
        }
      ]
    }
  }
]

resource ErrorLogAlerts 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (turnOnAlerts) {
  name: 'Error-Log-Alert'
  location: location
  properties: {
    displayName: 'Error Log Alert'
    description: 'Log alert for errors'
    enabled: true
    criteria: {
      allOf: [
        {
          query: '''
            ContainerAppConsoleLogs_CL
            | where Log_s contains "Error"
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
        }
      ]
    }
    actions: {
      actionGroups: [supportTeamActionGroup.id]
    }
    severity: 2 // Medium severity
    scopes: [
      logAnalyticsWorkspaceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
  }
}

resource WarningLogAlerts 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (turnOnAlerts) {
  name: 'Warning-Log-Alert'
  location: location
  properties: {
    displayName: 'Warning Log Alert'
    description: 'Log alert for warnings'
    enabled: true
    criteria: {
      allOf: [
        {
          query: '''
            ContainerAppConsoleLogs_CL
            | where Log_s contains "Warning"
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
        }
      ]
    }
    actions: {
      actionGroups: [supportTeamActionGroup.id]
    }
    severity: 3 // Low severity
    scopes: [
      logAnalyticsWorkspaceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
  }
}

resource ContainerTerminated 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (turnOnAlerts) {
  name: 'Container-Terminated-Alert'
  location: location
  properties: {
    displayName: 'Warning Log Alert'
    description: 'Log alert for warnings'
    enabled: true
    criteria: {
      allOf: [
        {
          query: '''
            ContainerAppSystemLogs_CL
            | where Log_s contains "readiness probe failed: connection refused" 
            | where TimeGenerated > ago(5m) 
            | project TimeGenerated, ContainerAppName_s, Log_s
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
        }
      ]
    }
    actions: {
      actionGroups: [supportTeamActionGroup.id]
    }
    severity: 2 // Medium severity
    scopes: [
      logAnalyticsWorkspaceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
  }
}
