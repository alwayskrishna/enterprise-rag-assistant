using System.Net.Http;
using Azure.Core;
using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class CloudAuthenticationTests
{
    private sealed class FakeCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            new("test-token", DateTimeOffset.UtcNow.AddMinutes(10));
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            ValueTask.FromResult(GetToken(requestContext, cancellationToken));
    }

    [Fact]
    public async Task ApiKeyModeAddsKeyHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        await new ApiKeyAuthenticator("test-key").AuthenticateAsync(request);
        Assert.Equal("test-key", Assert.Single(request.Headers.GetValues("api-key")));
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task EntraModeAddsBearerHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        await new EntraIdAuthenticator(new FakeCredential(), "https://cognitiveservices.azure.com/.default").AuthenticateAsync(request);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
        Assert.False(request.Headers.Contains("api-key"));
    }
}
