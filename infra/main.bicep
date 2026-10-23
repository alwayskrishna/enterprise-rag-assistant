targetScope = 'resourceGroup'

@description('Azure region for deployment.')
param location string = resourceGroup().location

@description('Globally unique ACR registry name, 5-50 alphanumeric characters.')
@minLength(5)
@maxLength(50)
param registryName string

@description('Container Apps environment name.')
param environmentName string = 'rag-env'

@description('Container App name.')
param appName string = 'rag-api'

@description('Image repository and tag in the newly created ACR (push image before deployment).')
param imageRepository string = 'enterprise-rag-api'
param imageTag string = 'latest'

@description('Allow public HTTP ingress. Default false: API is not ready for unrestricted internet exposure.')
param enableExternalIngress bool = false

@description('Run offline lexical API by default. Azure mode requires additional app settings and provisioned services.')
@allowed([
  'offline'
  'azure'
])
param ragBackend string = 'offline'

@description('Microsoft Entra tenant ID, required when ragBackend is azure.')
param azureTenantId string = ''

@description('Audience for incoming Entra ID JWTs, required when ragBackend is azure.')
param azureApiAudience string = ''

@description('Azure AI Search endpoint, required when ragBackend is azure.')
param azureSearchEndpoint string = ''

@description('Azure AI Search base index prefix. Tenant-specific indexes must already exist.')
param azureSearchIndex string = ''

@description('Azure OpenAI endpoint, required when ragBackend is azure.')
param azureOpenAiEndpoint string = ''

@description('Azure OpenAI chat deployment name, required when ragBackend is azure.')
param azureOpenAiDeployment string = ''

@description('Azure OpenAI embedding deployment name, required when ragBackend is azure.')
param azureEmbeddingDeployment string = ''

var registryServer = '${registryName}.azurecr.io'
var imageName = '${registryServer}/${imageRepository}:${imageTag}'
var cloudEnv = ragBackend == 'azure' ? [
  { name: 'RAG_BACKEND', value: 'azure' }
  { name: 'AZURE_AUTH_MODE', value: 'entra' }
  { name: 'AZURE_TENANT_ID', value: azureTenantId }
  { name: 'AZURE_API_AUDIENCE', value: azureApiAudience }
  { name: 'AZURE_SEARCH_ENDPOINT', value: azureSearchEndpoint }
  { name: 'AZURE_SEARCH_INDEX', value: azureSearchIndex }
  { name: 'AZURE_OPENAI_ENDPOINT', value: azureOpenAiEndpoint }
  { name: 'AZURE_OPENAI_DEPLOYMENT', value: azureOpenAiDeployment }
  { name: 'AZURE_OPENAI_EMBEDDING_DEPLOYMENT', value: azureEmbeddingDeployment }
] : [
  { name: 'RAG_BACKEND', value: 'offline' }
]

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${appName}-identity'
  location: location
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, 'AcrPull')
  scope: registry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: { destination: 'none' }
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: enableExternalIngress
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'rag-api'
          image: imageName
          env: cloudEnv
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/health', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
  dependsOn: [acrPull]
}

output registryLoginServer string = registry.properties.loginServer
output containerAppName string = app.name
output managedIdentityPrincipalId string = identity.properties.principalId
output ingressFqdn string = enableExternalIngress ? app.properties.configuration.ingress.fqdn : ''
