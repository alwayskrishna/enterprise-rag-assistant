# Optional Azure OpenAI grounded answers

The local CLI defaults to evidence-only excerpts and never calls a model.

To enable optional generated answers, configure these environment variables in your shell (never commit secrets):

- `AZURE_OPENAI_ENDPOINT` — HTTPS Azure OpenAI endpoint, e.g. `https://example.openai.azure.com/`
- `AZURE_OPENAI_DEPLOYMENT` — existing chat model deployment name
- `AZURE_OPENAI_API_KEY` — Azure OpenAI API key

Run:

```bash
dotnet run --project src/EnterpriseRag -- ask "How should latency incidents be handled?" --ai
```

The `--ai` switch is opt-in and may incur Azure usage charges. Retrieval uses the local token-frequency baseline; it is **not** yet Azure OpenAI embedding search. The model receives only the top-ranked excerpts and instructions to cite them. Generated claims are not automatically verified against the sources; human review remains necessary.

If retrieval returns no evidence, the service abstains without calling the model. Never ingest confidential production documents without authorization and access controls. This demo does not implement tenant isolation, secret vault integration, or prompt-injection detection.
