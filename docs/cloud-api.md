# Azure-backed RAG HTTP API

The API defaults to offline lexical retrieval. Set `RAG_BACKEND=azure` to enable cloud-backed endpoints with Azure OpenAI embeddings and grounded generation plus Azure AI Search vector storage.

Required settings:

- `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_EMBEDDING_DEPLOYMENT`, `AZURE_OPENAI_DEPLOYMENT`
- `AZURE_SEARCH_ENDPOINT`, `AZURE_SEARCH_INDEX`
- `AZURE_AUTH_MODE=entra` with Azure RBAC roles, **or** `AZURE_OPENAI_API_KEY` and `AZURE_SEARCH_API_KEY`

Provision the Azure AI Search index schema as described in [Azure AI Search setup](azure-ai-search.md).

In cloud mode:
- `POST /documents` chunks text, generates embeddings, and upserts into Azure AI Search
- `POST /ask` embeds the question, retrieves relevant chunks, and generates a grounded answer with source metadata
- `DELETE /documents/{id}` removes indexed chunks
- `GET /documents` returns HTTP 501 because the vector-index abstraction has no list operation
- `GET /health` checks only local process liveness; it does not verify cloud dependencies

Cloud requests can incur charges. There is no user authentication, per-tenant isolation, document-level access control, or durable ingestion job queue. Do not expose this demo API publicly or index sensitive documents without adding those controls. Cloud-generated citations are instructed but not independently verified.
