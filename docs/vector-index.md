# Persistent vector index

The local `FileVectorIndex` stores precomputed document embeddings in a JSON file and reloads them across process restarts. Use `index` to embed and upsert a document, and `search` to query the saved index:

```bash
dotnet run --project src/EnterpriseRag -- index samples/operations.txt --index-file .data/vectors.json
dotnet run --project src/EnterpriseRag -- search "How do we respond to latency?" --index-file .data/vectors.json
dotnet run --project src/EnterpriseRag -- delete operations.txt --index-file .data/vectors.json
```

Configure `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY` and `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` before running these commands. The index stores vectors; only the query is embedded during search.

The file index is a **local prototype**. It does not provide cross-process concurrency, tenant isolation, encryption at rest, access control, vector schema migration or approximate-nearest-neighbor indexing. Never store sensitive documents without an approved storage and access-control design. An Azure AI Search adapter is planned separately; this PR does not claim cloud vector indexing.
