using System.Net;
using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public class CloudResilienceTests
{
    private sealed class Handler(Func<int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(++Calls));
    }

    [Fact]
    public async Task RetriesTransientErrorThenSucceeds()
    {
        var handler = new Handler(call => new HttpResponseMessage(call == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        using var response = await CloudResilience.SendAsync(client, () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task DoesNotRetryAuthenticationFailure()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = new HttpClient(handler);
        using var response = await CloudResilience.SendAsync(client, () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task HonorsCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using var client = new HttpClient(new Handler(_ => throw new Exception("Should not send")));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CloudResilience.SendAsync(client, () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"), source.Token));
    }
}
