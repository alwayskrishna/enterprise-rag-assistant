# Azure Container Apps deployment (Bicep)

This infrastructure template creates Azure Container Registry (Basic), a user-assigned managed identity, AcrPull role assignment, a Container Apps environment and a Container App. It does **not** provision Azure OpenAI or Azure AI Search. The default deployment uses the offline lexical API with **internal ingress**; no public endpoint is exposed.

## Prerequisites

- Azure CLI with Bicep support, Docker and an Azure subscription
- Permissions to create resource-group resources and role assignments
- A globally unique registry name; adjust `infra/main.bicepparam`
- Billing approval: Azure Container Apps, Container Registry and optional Azure AI services can incur charges

## Validate infrastructure

```bash
az bicep build --file infra/main.bicep
az deployment group validate --resource-group <resource-group> --template-file infra/main.bicep --parameters infra/main.bicepparam
```

## Publish image and deploy

The image must exist in ACR **before** the Container App is deployed. Provision the registry first with:

```bash
az acr create --resource-group <resource-group> --name <unique-acr-name> --sku Basic --admin-enabled false
az acr build --registry <unique-acr-name> --image enterprise-rag-api:latest --file Dockerfile.api .
az deployment group create --resource-group <resource-group> --template-file infra/main.bicep --parameters infra/main.bicepparam
```

The template references the existing ACR declaratively; its configuration must match. A role-assignment propagation delay may require retrying the deployment.

## Azure-backed mode

Set `ragBackend=azure`, `azureTenantId`, `azureApiAudience`, `azureSearchEndpoint`, `azureSearchIndex`, `azureOpenAiEndpoint`, `azureOpenAiDeployment`, and `azureEmbeddingDeployment` in a **private** parameter file. The application uses `AZURE_AUTH_MODE=entra` and its user-assigned managed identity.

**Identity configuration caveat:** Azure.Identity's DefaultAzureCredential in Container Apps must be directed to the user-assigned identity with `AZURE_CLIENT_ID` (the identity's client ID). The Bicep template configures this automatically only after the identity is created.

Assign the identity Azure AI Search index data permissions and Azure OpenAI inference permissions separately. Provision each tenant-specific Azure AI Search index beforehand, using the documented schema. Cloud mode validates incoming Entra ID JWTs and requires Rag.Read/Rag.Write scope or app-role authorization, but does not implement per-user document ACLs.

## Safety

Keep `enableExternalIngress=false` until you complete a secure ingress/network review, add gateway-level protection and distributed rate limits, and obtain an approved deployment security review. Application-level scopes and process-local rate limiting are already implemented. The offline API is unauthenticated. Do not commit secrets or credentials. The default Container Apps environment has logging disabled; enable appropriate telemetry before production use.
