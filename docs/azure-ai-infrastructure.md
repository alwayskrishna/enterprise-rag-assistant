# Provision Azure AI dependencies

`infra/ai.bicep` provisions an Azure AI Search Basic service, an Azure OpenAI account, chat and embedding model deployments, and data-plane role assignments for the user-assigned identity from `infra/main.bicep`.

**Important:** This is an optional, billable deployment. Model availability, deployment versions, capacity and regional quota must be checked before deployment. Azure OpenAI account creation can require prior approval. The template uses public service endpoints; restrict networking before production.

## Deployment order

1. Deploy `infra/main.bicep` in offline mode after pushing the container image to ACR. This creates the identity named `rag-api-identity`.
2. Validate `infra/ai.bicep` with `az bicep build --file infra/ai.bicep`. Deploy it with:

```bash
az deployment group create --resource-group <rg> --template-file infra/ai.bicep --parameters \
  searchServiceName=<unique-search-name> openAiAccountName=<unique-openai-name> \
  tenantId=<your-entra-tenant-guid>
```

3. Provision a tenant-specific search index using the supplied schema. The index name must equal the SHA-256-derived value used by `TenantSecurity.IndexName`. Example:

```bash
export AZURE_SEARCH_ENDPOINT=https://<search-name>.search.windows.net/
export AZURE_SEARCH_INDEX=rag
export AZURE_TENANT_ID=<tenant-guid>
export AZURE_SEARCH_API_KEY=<admin-key>
bash scripts/provision-search-index.sh
unset AZURE_SEARCH_API_KEY
```

The provisioning script uses an admin key **only for index creation**. The running API uses managed identity (`AZURE_AUTH_MODE=entra`). Never commit or log the admin key.

4. Redeploy `infra/main.bicep` with `ragBackend=azure` and matching OpenAI/Search endpoints, model deployment names, JWT audience and tenant. The identity must have access to the AI services and its ACR image. RBAC propagation can take time.

## Caveats

The app currently validates a **single Entra tenant** per deployment. Each tenant has a separate index, but the API lacks app-role/scope authorization and document ACLs. The template creates public Azure AI endpoints and no private endpoints, budget controls, or audit logging. Do not expose the API publicly without further hardening. The search index schema's vector dimensions must match the actual embedding model output.
