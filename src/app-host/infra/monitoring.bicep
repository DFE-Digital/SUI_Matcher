param actionGroupName string = 'On-Call Team'
// To handle the fact that bicep does not deploy the apps so the containers won't exist
// until after the first deployment. So this needs to be a phased approach to monitoring.
param turnOnAlerts bool = true
param location string = resourceGroup().location
param logAnalyticsWorkspaceId string
param actionGroupEmail string

module monitoring '../../../infra/modules/shared/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    actionGroupName: actionGroupName
    turnOnAlerts: turnOnAlerts
    location: location
    logAnalyticsWorkspaceId: logAnalyticsWorkspaceId
    actionGroupEmail: actionGroupEmail
  }
}
