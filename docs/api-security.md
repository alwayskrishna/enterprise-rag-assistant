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

## Authorization and throttling (PR #16)

Cloud API access tokens must contain a delegated `scp` claim or application `roles` claim with `Rag.Read` (for `GET /documents` and `POST /ask`) or `Rag.Write` (for `POST /documents` and `DELETE /documents/{id}`). Define and grant these permissions in the API's Entra app registration; a valid JWT alone is insufficient. Unauthorized callers receive HTTP 403.

Cloud endpoints are limited to 60 requests per minute per authenticated object ID, falling back to client IP when unavailable. Exceeding the limit returns HTTP 429. This is a **process-local** rate limiter, not a distributed quota: multiple replicas require enforcement at an API gateway. Document IDs permit ASCII letters, digits, dots, dashes and underscores (maximum 200 characters); questions are limited to 4,096 characters, and document text to 1,000,000 characters. These are basic controls, not comprehensive abuse protection.

The service still needs secure network ingress, WAF/API gateway protection, request size limits at the reverse proxy, app registration permission governance, security testing, and per-user document authorization before production deployment.
