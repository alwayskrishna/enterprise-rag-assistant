using './main.bicep'

param registryName = 'replacewithuniqueacrname'
param environmentName = 'rag-env'
param appName = 'rag-api'
param imageRepository = 'enterprise-rag-api'
param imageTag = 'latest'
param enableExternalIngress = false
param ragBackend = 'offline'
