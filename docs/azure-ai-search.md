# Azure AI Search vector backend (opt-in)

This adapter implements `IVectorIndex` using Azure AI Search REST APIs. It requires an **existing** search index with the following fields:

| Field | Type | Requirements |
|---|---|---|
| `id` | Edm.String | key |
| `documentId` | Edm.String | filterable |
| `chunkIndex` | Edm.Int32 | retrievable |
| `text` | Edm.String | searchable, retrievable |
| `embedding` | Collection(Edm.Single) | vectorSearchDimensions matching your embedding deployment; vectorSearchProfile assigned |

Provision the vector search algorithm/profile and index schema in Azure before use. Index dimensions must match the embedding model output. This PR does not automatically create cloud resources.

Set `AZURE_SEARCH_ENDPOINT` (e.g. `https://example.search.windows.net/`), `AZURE_SEARCH_INDEX`, and `AZURE_SEARCH_API_KEY`. The adapter accepts an admin key with write access; keep secrets out of source control.

Run `index ... --azure-search`, `search ... --azure-search`, or `delete ... --azure-search`. Index and search commands also require the Azure OpenAI embedding configuration documented in [semantic search](semantic-search.md).

**Caveats:** This is a demo adapter, not a hardened production service. Replace-by-delete is non-atomic, writes can be eventually consistent, and the adapter has no tenant authorization, retry/backoff, schema provisioning, or private-network setup. Do not use it with sensitive documents until identity, access, data isolation, and operational controls are implemented.
