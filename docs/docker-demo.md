# Docker demo (no Azure account required)

From the repository root:

```bash
docker build -t enterprise-rag-assistant:local .
docker run --rm enterprise-rag-assistant:local ask "How should latency incidents be handled?"
docker run --rm enterprise-rag-assistant:local eval evals/cases.json samples/operations.txt
```

Or use `bash scripts/demo.sh`.

The Dockerfile uses a .NET 10 SDK build stage and a smaller .NET runtime stage, runs as a non-root user, and includes the sample document and evaluation cases. The image is a CLI container, **not an HTTP service**; no ports need to be exposed.

To query your own documents, mount a read-only directory and pass its path:

```bash
docker run --rm -v "$PWD/mydocs:/documents:ro" enterprise-rag-assistant:local ask "What does the policy say?" /documents/policy.pdf
```

For Azure-backed features, inject environment variables securely at runtime (do not bake credentials into the image). Managed identity requires an appropriate Azure host and role assignments. The local demo uses deterministic lexical retrieval, not an LLM, and incurs no Azure API charges.
