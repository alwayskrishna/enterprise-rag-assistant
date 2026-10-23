using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EnterpriseRag.Api;

public static class ApiTelemetry
{
    public const string SourceName = "EnterpriseRag.Api";
    public static readonly ActivitySource Source = new(SourceName);
    public static readonly Meter Meter = new(SourceName);
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("rag.api.requests");
    public static readonly Counter<long> Failures = Meter.CreateCounter<long>("rag.api.failures");
    public static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("rag.api.duration", unit: "ms");
}
