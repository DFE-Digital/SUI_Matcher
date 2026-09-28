@description('The location used for all deployed resources')
param location string

@description('The prefix used for all deployed resources')
param environmentPrefix string

@description('The lowercase environment name used for resource naming')
param lowercaseEnvironmentName string

@description('Short stack-specific suffix used to avoid cross-stack name collisions.')
param stackNameSuffix string = ''

@description('The private IPv4 address of the next-hop network appliance (e.g. a firewall) for the default route')
param nextHopIpAddress string

@description('Tags that will be applied to all resources')
param tags object = {}

var stackNameToken = empty(stackNameSuffix) ? '' : '-${toLower(stackNameSuffix)}'
var routeTableName = '${environmentPrefix}-${lowercaseEnvironmentName}${stackNameToken}-rt-01'

// Validation
var parsedNextHop = parseCidr('${nextHopIpAddress}/32')
var nextHopAddress = contains(parsedNextHop.network, '.')
  ? parsedNextHop.network
  : fail('nextHopIpAddress must be a valid IPv4 address.')

resource routeTable 'Microsoft.Network/routeTables@2024-05-01' = {
  name: routeTableName
  location: location
  tags: tags
  properties: {
    disableBgpRoutePropagation: true
    routes: [
      {
        name: 'DefaultToFirewall'
        properties: {
          addressPrefix: '0.0.0.0/0'
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: nextHopAddress
        }
        type: 'Microsoft.Network/routeTables/routes'
      }
    ]
  }
}

output routeTableId string = routeTable.id
output routeTableName string = routeTable.name
