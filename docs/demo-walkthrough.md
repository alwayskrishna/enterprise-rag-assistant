# Demo walkthrough (5–7 minutes)

This demo uses **offline mode** and requires no Azure account.

## 1. Explain the architecture (45 seconds)

Show [architecture.md](architecture.md). Explain chunking → retrieval → cited evidence, with optional Azure embeddings, AI Search, and grounded answer generation.

## 2. Show the CLI and retrieval evaluation (90 seconds)

```bash
dotnet run --project src/EnterpriseRag -- ask "How should latency incidents be handled?"
dotnet run --project src/EnterpriseRag -- eval evals/cases.json samples/operations.txt
```

Point out the source references. Explain that lexical vectors are deterministic and semantic retrieval is an optional cloud capability.

## 3. Demonstrate the HTTP API (2 minutes)

In one terminal:

```bash
dotnet run --project src/EnterpriseRag.Api --urls http://localhost:5080
```

In another:

```bash
curl -fsS http://localhost:5080/health
curl -fsS -X POST http://localhost:5080/documents -H 'Content-Type: application/json' -d '{"id":"incident-runbook","text":"When worker queue latency rises, inspect backlog and retry failures, then escalate to on-call."}'
curl -fsS -X POST http://localhost:5080/ask -H 'Content-Type: application/json' -d '{"question":"What should we do about queue latency?","topK":3}'
curl -fsS http://localhost:5080/documents
curl -fsS -X DELETE http://localhost:5080/documents/incident-runbook
```

Explain the offline API is a local demonstration, not a publicly secured endpoint.

## 4. Demonstrate reliability and test coverage (60 seconds)

```bash
dotnet test tests/EnterpriseRag.Tests/EnterpriseRag.Tests.csproj -c Release
python3 scripts/load-test-api.py --requests 40 --concurrency 5
```

Run the load test while the local API remains running. Explain its measured p50/p95 and request throughput are **local measurements only**; do not present them as production SLAs.

## 5. Walk through deployment and security (60 seconds)

Show `.github/workflows/ci.yml`, `.github/workflows/deploy-azure.yml`, `infra/main.bicep`, `infra/ai.bicep`, and `docs/api-security.md`. Explain GitHub OIDC, manual environment approval, tenant-specific indexes, JWT permission checks, telemetry and cost controls.

## Common questions

**Why not always use an LLM?** A deterministic baseline allows testing without cost and provides a fallback for simple lexical matches.

**How do you isolate tenants?** Cloud API validates the JWT and selects a tenant-derived Azure AI Search index. A single configured issuer currently limits the deployment to one trusted tenant.

**How do you handle failures?** Retry only transient cloud HTTP failures with backoff; surface non-transient errors and cancellation.

**What's missing for production?** Per-user ACLs, distributed throttling, WAF/gateway, real Azure integration/chaos tests, and measured SLOs.
