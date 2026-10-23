using System.Net;
using System.Text;
using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public class AzureAiSearchVectorIndexTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handle(request));
    }

    [Fact]
    public async Task VectorQueryReturnsSourceAttributedHits()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler(request =>
        {
            calls++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Contains("/docs/search", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"value\":[{\"documentId\":\"runbook\",\"chunkIndex\":2,\"text\":\"Inspect queue\",\"@search.score\":0.92}]}",
                    Encoding.UTF8, "application/json")
            };
        }));
        var index = new AzureAiSearchVectorIndex(client, new Uri("https://demo.search.windows.net/"), "runbooks", "test-key");
        var hits = await index.SearchAsync(new[] { 1f, 0f });
        Assert.Equal(1, calls);
        Assert.Equal("runbook", Assert.Single(hits).DocumentId);
        Assert.Equal(2, hits[0].ChunkIndex);
    }

    [Fact]
    public async Task EmptyQueryVectorIsRejectedBeforeNetworkCall()
    {
        using var client = new HttpClient(new Handler(_ => throw new Exception("Unexpected request")));
        var index = new AzureAiSearchVectorIndex(client, new Uri("https://demo.search.windows.net/"), "runbooks", "test-key");
        await Assert.ThrowsAsync<ArgumentException>(() => index.SearchAsync(Array.Empty<float>()));
    }

    [Fact]
    public async Task DeletingUnknownDocumentDoesNotWrite()
    {
        var calls = 0;
        using var client = new HttpClient(new Handler(request =>
        {
            calls++;
            Assert.Contains("/docs/search", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":[]}", Encoding.UTF8, "application/json")
            };
        }));
        var index = new AzureAiSearchVectorIndex(client, new Uri("https://demo.search.windows.net/"), "runbooks", "test-key");
        await index.DeleteDocumentAsync("missing");
        Assert.Equal(1, calls);
    }
}
