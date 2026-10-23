targetScope = 'resourceGroup'

param location string = resourceGroup().location
param workspaceName string = 'rag-logs'
param insightsName string = 'rag-appinsights'
@description('Azure Monitor action group resource ID for notifications; empty disables actions.')
param actionGroupId string = ''

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    retentionInDays: 30
    sku: { name: 'PerGB2018' }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
  }
}

resource failedRequests 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'rag-api-failed-requests'
  location: 'global'
  properties: {
    description: 'Alert when Application Insights reports HTTP server failures.'
    severity: 2
    enabled: true
    scopes: [insights.id]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'FailedRequests'
          metricName: 'requests/failed'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: empty(actionGroupId) ? [] : [
      { actionGroupId: actionGroupId }
    ]
  }
}

output connectionString string = insights.properties.ConnectionString
output applicationInsightsId string = insights.id
output logAnalyticsWorkspaceId string = workspace.id
