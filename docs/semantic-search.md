# Semantic search (opt-in)

The default retrieval method uses normalized token frequencies. Add `--semantic` to generate neural embeddings through an Azure OpenAI embedding deployment.

Set:
- `AZURE_OPENAI_ENDPOINT` — e.g. `https://example.openai.azure.com/`
- `AZURE_OPENAI_API_KEY` — secret; never commit
- `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` — embedding model deployment name
- `AZURE_OPENAI_DEPLOYMENT` — chat model deployment, **only** if combining `--semantic --ai`

Examples:

```bash
dotnet run --project src/EnterpriseRag -- ask "How do we address processing delays?" --semantic
dotnet run --project src/EnterpriseRag -- ask "How do we address processing delays?" --semantic --ai
```

`--semantic` makes remote API requests and can incur charges. Embeddings are generated afresh per query and chunk; this initial milestone does **not** persist or cache vectors. It is intended for small demos, not large document sets. Future milestones will add a vector index and batching.

Semantic similarity measures vector proximity, **not** factual correctness or authorization. Always review generated answers and citations. CI uses fake embeddings and does not contact Azure.
