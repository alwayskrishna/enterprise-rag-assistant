# Integration, load and resilience verification

## Automated checks

- Unit/integration-style tests exercise document ingestion, cited retrieval, replacement, deletion and concurrent reads/writes using the in-memory DocumentStore.
- Existing cloud resilience tests simulate HTTP 429 retries, HTTP 401 no-retry behavior and cancellation with a fake HTTP handler.
- CI starts the API Docker image, exercises document and answer endpoints, and runs a **local-only** bounded HTTP load smoke test (40 requests, 5 workers).
- Retrieval accuracy evaluation remains gated by `evals/cases.json`.

## Run locally

```bash
dotnet test tests/EnterpriseRag.Tests/EnterpriseRag.Tests.csproj -c Release
docker build -f Dockerfile.api -t rag-api:local .
docker run --rm -d --name rag-api-load -p 5080:8080 rag-api:local
curl -fsS -X POST http://localhost:5080/documents -H 'Content-Type: application/json' -d '{"id":"runbook","text":"worker queue latency incident"}'
python3 scripts/load-test-api.py --requests 100 --concurrency 10
docker stop rag-api-load
```

## Interpretation and limitations

This is a repeatable **smoke benchmark**, not a formal capacity test or SLA guarantee. CI runner contention can change throughput and latency. The script intentionally refuses non-loopback targets to prevent accidental load against public services. Tests do not provision Azure resources, validate Entra JWTs end to end, or simulate real Azure AI Search/OpenAI outages. Those require a dedicated secured staging environment and explicit approval.
