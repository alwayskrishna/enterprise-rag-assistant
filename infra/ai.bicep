targetScope = 'resourceGroup'

param location string = resourceGroup().location
@description('Globally unique Azure AI Search service name.')
param searchServiceName string
@description('Globally unique Azure OpenAI resource name.')
param openAiAccountName string
@description('User-assigned identity resource name created by main.bicep.')
param identityName string = 'rag-api-identity'
@description('Deployment name for chat completions.')
param chatDeploymentName string = 'rag-chat'
@description('Chat model name and version must be available in the selected region and subscription.')
param chatModelName string = 'gpt-4o-mini'
param chatModelVersion string = '2024-07-18'
@description('Deployment name for embeddings.')
param embeddingDeploymentName string = 'rag-embedding'
param embeddingModelName string = 'text-embedding-3-small'
param embeddingModelVersion string = '1'
@description('Tenant GUID used to derive the isolated search index name.')
param tenantId string
@description('Index prefix must match AZURE_SEARCH_INDEX.')
param searchIndexPrefix string = 'rag'
@description('Vector dimensions must match the embedding deployment.')
param embeddingDimensions int = 1536

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityName
}

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchServiceName
  location: location
  sku: { name: 'basic' }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

resource openAi 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiAccountName
  location: location
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: openAiAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAi
  name: chatDeploymentName
  sku: { name: 'Standard', capacity: 1 }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
  }
}

resource embeddings 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAi
  name: embeddingDeploymentName
  sku: { name: 'Standard', capacity: 1 }
  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModelName
      version: embeddingModelVersion
    }
  }
}

resource searchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, identity.id, 'Search Index Data Contributor')
  scope: search
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
  }
}

resource openAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAi.id, identity.id, 'Cognitive Services OpenAI User')
  scope: openAi
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  }
}

output searchEndpoint string = 'https://${search.name}.search.windows.net/'
output openAiEndpoint string = 'https://${openAi.name}.openai.azure.com/'
output configuredIndexPrefix string = searchIndexPrefix
output tenantId string = tenantId
output embeddingDimensions int = embeddingDimensions
