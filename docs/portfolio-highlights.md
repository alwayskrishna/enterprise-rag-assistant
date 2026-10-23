# Portfolio presentation and interview notes

## One-minute project pitch

Built a .NET 10/C# retrieval-augmented generation system with an offline, deterministic document-search baseline and optional Azure OpenAI and Azure AI Search integrations. Designed reusable ingestion, chunking, embedding, vector indexing and grounded-answer components; exposed CLI and HTTP interfaces; added Entra ID authentication, permission checks, tenant-derived search indexes, OpenTelemetry instrumentation and bounded cloud retries. Automated build, evaluation and local API tests, with an approval-gated GitHub OIDC deployment workflow for Azure Container Apps. Azure services are optional and deployment is not asserted to have been executed.

## Engineering areas demonstrated

- **Distributed systems and cloud:** Azure AI Search integration, Container Apps and Bicep, managed identities, OIDC deployment, cloud retry patterns.
- **Backend architecture:** .NET 10, ASP.NET Core Minimal APIs, dependency abstractions, local and cloud backends.
- **AI engineering:** document extraction, chunking, embeddings, retrieval, grounded generation, offline evaluation.
- **Reliability:** OpenTelemetry traces/metrics, alert infrastructure, deterministic tests, local API load smoke testing.
- **Security:** Entra JWT validation, delegated scopes/application roles, tenant-index isolation, basic input validation and throttling.

## Responsible claims

Describe what is **implemented**, not what is unverified. Do not claim a live production deployment, a specific number of users, p95 production latency, validated hallucination reduction, full multitenant authorization, or cost savings without measurements. Explain the difference between the local demo and optional cloud infrastructure.

## Suggested next improvements beyond this portfolio milestone

Provision a secure staging environment, run real Azure integration tests, measure answer relevance and grounding on a larger labeled dataset, add document-level access controls, use gateway-enforced distributed rate limits, and document recovery drills.
