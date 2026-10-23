# Observability and monitoring

The ASP.NET Core API emits OpenTelemetry HTTP server traces and metrics plus the existing `EnterpriseRag.Cloud` ActivitySource/Meter for outbound Azure HTTP requests. API middleware records request counts, errors, and latency by route template and status code. It deliberately avoids recording question text, document contents, credentials, and tenant IDs.

Set `APPLICATIONINSIGHTS_CONNECTION_STRING` to enable Azure Monitor OpenTelemetry export. Without it, instrumentation runs locally without cloud export.

Infrastructure: `infra/monitoring.bicep` provisions Log Analytics, workspace-based Application Insights, and Azure Monitor metric alerts for failed requests and request duration. Alerts require actual traffic and should be tuned against baseline behavior.

**Caveats:** No production SLO is asserted; alert thresholds are starting points. A request failure is an HTTP 5xx; authorization errors are counted separately in HTTP metrics. Instrumentation must not include raw prompt or document text. Configure sampling, retention, RBAC and cost limits before production use.
