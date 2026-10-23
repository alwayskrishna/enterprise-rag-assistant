# ASP.NET Core HTTP API (offline demo)

The API is an in-memory, lexical-retrieval demonstration. It does **not** call Azure services, persist documents, authenticate clients, or generate LLM answers.

Start from the repository root:

```bash
dotnet run --project src/EnterpriseRag.Api --urls http://localhost:5080
```

Then use:

```bash
curl http://localhost:5080/health
curl -X POST http://localhost:5080/documents -H 'Content-Type: application/json' -d '{"id":"runbook","text":"For latency incidents inspect queue depth, worker health and retry rates."}'
curl -X POST http://localhost:5080/ask -H 'Content-Type: application/json' -d '{"question":"How do I inspect queue latency?","topK":3}'
curl http://localhost:5080/documents
curl -X DELETE http://localhost:5080/documents/runbook
```

Endpoints: `GET /health`, `GET /documents`, `POST /documents`, `DELETE /documents/{id}`, and `POST /ask`.

Document text is limited to 1,000,000 characters; IDs to 200 characters; `topK` to 1–20. Documents are lost on restart. The API should not be exposed publicly until authentication, authorization, request-size enforcement at the host/proxy, rate limiting, and durable tenant-isolated storage are implemented.
