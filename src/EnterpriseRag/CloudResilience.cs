using System.Diagnostics;
using System.Net;

namespace EnterpriseRag;

/// <summary>Bounded retries for transient HTTP failures. Each attempt creates a fresh request.</summary>
public static class CloudResilience
{
    public static readonly ActivitySource ActivitySource = new("EnterpriseRag.Cloud");
    public static readonly MeteringCounters Counters = new();

    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default, int maxAttempts = 3)
    {
        if (maxAttempts < 1 || maxAttempts > 5) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        cancellationToken.ThrowIfCancellationRequested();
        using var activity = ActivitySource.StartActivity("cloud.http", ActivityKind.Client);
        var timer = Stopwatch.StartNew();
        try
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = requestFactory();
                try
                {
                    var response = await client.SendAsync(request, cancellationToken);
                    if (!IsTransient(response.StatusCode) || attempt == maxAttempts)
                    {
                        activity?.SetTag("http.response.status_code", (int)response.StatusCode);
                        return response;
                    }
                    response.Dispose();
                }
                catch (HttpRequestException) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
                {
                    // Transient network failure; retry with a fresh request.
                }
                Counters.Retries.Add(1);
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (1 << (attempt - 1))), cancellationToken);
            }
            throw new InvalidOperationException("Unexpected retry loop termination.");
        }
        finally
        {
            Counters.Requests.Add(1);
            Counters.Duration.Record(timer.Elapsed.TotalMilliseconds);
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.RequestTimeout || status == HttpStatusCode.TooManyRequests || (int)status >= 500;
}
