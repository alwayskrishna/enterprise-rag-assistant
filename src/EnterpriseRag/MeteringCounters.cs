using System.Diagnostics.Metrics;

namespace EnterpriseRag;

public sealed class MeteringCounters
{
    private static readonly Meter Meter = new("EnterpriseRag.Cloud", "1.0");
    public Counter<long> Requests { get; } = Meter.CreateCounter<long>("rag.cloud.requests");
    public Counter<long> Retries { get; } = Meter.CreateCounter<long>("rag.cloud.retries");
    public Histogram<double> Duration { get; } = Meter.CreateHistogram<double>("rag.cloud.duration", "ms");
}
