# HTTP API authentication and tenant isolation

Cloud mode (`RAG_BACKEND=azure`) requires validated Microsoft Entra ID bearer tokens on document and question endpoints. Set `AZURE_TENANT_ID` (a single allowed tenant GUID) and `AZURE_API_AUDIENCE` (the API application ID URI or audience). The API validates signature, issuer, audience, lifetime, and tenant, using Entra ID discovery and signing keys.

Every tenant gets a separate Azure AI Search index: `AZURE_SEARCH_INDEX-<first 16 hex digits of SHA-256(tenant GUID)>`. The tenant comes from the **validated JWT `tid` claim**, never a request field. You must provision each tenant-specific index with the schema in [Azure AI Search setup](azure-ai-search.md). An existing shared index is **not** used in cloud mode. Assign the API's managed identity appropriate data-plane roles for those indexes.

Example:

```bash
export RAG_BACKEND=azure
export AZURE_TENANT_ID=11111111-1111-1111-1111-111111111111
export AZURE_API_AUDIENCE=api://YOUR-API-APP-ID
# Set Azure OpenAI and Azure Search configuration per docs/cloud-api.md
dotnet run --project src/EnterpriseRag.Api
```

Obtain an access token for your API application using the appropriate OAuth flow, then pass `Authorization: Bearer <token>`. The `/health` endpoint is anonymous and reports only process liveness.

**Important limitations:** Offline mode remains an unauthenticated local demo and must not be publicly exposed. Tenant-specific indexes must be provisioned outside the app. This PR isolates tenants by index and validates the token tenant but does not implement per-user document permissions, app-role/scope authorization, throttling, audit trails, or hardened internet deployment. The configured single-tenant issuer means only one tenant is accepted until a vetted multitenant trust model is added.
