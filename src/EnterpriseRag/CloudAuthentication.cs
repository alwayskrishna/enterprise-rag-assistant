using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;

namespace EnterpriseRag;

public interface ICloudRequestAuthenticator
{
    ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}

public sealed class ApiKeyAuthenticator(string key) : ICloudRequestAuthenticator
{
    public ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.Headers.Add("api-key", key);
        return ValueTask.CompletedTask;
    }
}

public sealed class EntraIdAuthenticator(TokenCredential credential, string scope) : ICloudRequestAuthenticator
{
    public async ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }
}

public static class CloudAuthentication
{
    public static ICloudRequestAuthenticator FromEnvironment(string apiKey, string scope)
    {
        var mode = Environment.GetEnvironmentVariable("AZURE_AUTH_MODE");
        if (string.Equals(mode, "entra", StringComparison.OrdinalIgnoreCase))
            return new EntraIdAuthenticator(new DefaultAzureCredential(), scope);
        if (!string.IsNullOrEmpty(mode) && !string.Equals(mode, "key", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("AZURE_AUTH_MODE must be 'key' or 'entra'.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API key is required when AZURE_AUTH_MODE is 'key'.");
        return new ApiKeyAuthenticator(apiKey);
    }
}
