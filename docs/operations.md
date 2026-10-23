# Cloud reliability and telemetry

The Azure OpenAI and Azure AI Search HTTP adapters use bounded retries for HTTP 408, 429, and 5xx errors, plus transient transport exceptions. Requests are recreated on every attempt. Authentication and other client errors are **not** retried. Retry delays are 100 ms and 200 ms; requests remain bounded by the CLI HttpClient timeout.

The `EnterpriseRag.Cloud` ActivitySource and Meter expose `cloud.http` spans and metrics:
- `rag.cloud.requests` (request count)
- `rag.cloud.retries` (retry count)
- `rag.cloud.duration` (duration in milliseconds)

No exporter is configured by default. Connect OpenTelemetry instrumentation/exporters in the hosting application to collect telemetry. Do not log API keys, prompts, raw document text, or full cloud URLs.

Security: credentials are read from environment variables and used only in request headers. This prototype still requires tenant isolation, access control, managed identity, secret rotation, request quotas, prompt-injection testing, and private network controls before handling sensitive production data.
