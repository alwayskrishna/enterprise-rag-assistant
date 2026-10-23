using System.Net.Http.Json;
using System.Text.Json;

namespace EnterpriseRag;

public interface IEmbeddingGenerator
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public static class SemanticSearch
{
    public static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
            throw new ArgumentException("Embedding dimensions must match and be nonzero.");
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }
        return normA == 0 || normB == 0 ? 0 : dot / Math.Sqrt(normA * normB);
    }

    public static async Task<IReadOnlyList<SearchHit>> SearchAsync(
        IEnumerable<Chunk> chunks, string query, IEmbeddingGenerator generator,
        int topK = 3, CancellationToken cancellationToken = default)
    {
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK));
        var sourceChunks = chunks.ToArray();
        if (sourceChunks.Length == 0) return Array.Empty<SearchHit>();
        var queryVector = await generator.EmbedAsync(query, cancellationToken);
        var results = new List<SearchHit>();
        foreach (var chunk in sourceChunks)
        {
            var vector = await generator.EmbedAsync(chunk.Text, cancellationToken);
            var similarity = CosineSimilarity(queryVector, vector);
            if (similarity > 0)
                results.Add(new SearchHit(chunk.DocumentId, chunk.Index, chunk.Text, similarity));
        }
        return results.OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.DocumentId, StringComparer.Ordinal)
            .ThenBy(hit => hit.ChunkIndex)
            .Take(topK).ToArray();
    }
}

public sealed class AzureOpenAiEmbeddingGenerator(HttpClient client, Uri endpoint, string deployment, string apiKey) : IEmbeddingGenerator
{
    public static AzureOpenAiEmbeddingGenerator FromEnvironment(HttpClient client)
    {
        var rawEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");
        var key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        if (!Uri.TryCreate(rawEndpoint, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !endpoint.Host.EndsWith(".openai.azure.com", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(deployment)
            || (Environment.GetEnvironmentVariable("AZURE_AUTH_MODE") != "entra" && string.IsNullOrWhiteSpace(key)))
            throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_EMBEDDING_DEPLOYMENT and AZURE_OPENAI_API_KEY.");
        return new AzureOpenAiEmbeddingGenerator(client, endpoint, deployment, key ?? string.Empty);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text must not be empty.", nameof(text));
        var url = new Uri(endpoint, $"openai/deployments/{Uri.EscapeDataString(deployment)}/embeddings?api-version=2024-10-21");
        using var response = await CloudResilience.SendAuthenticatedAsync(client, async ct =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            await CloudAuthentication.FromEnvironment(apiKey, "https://cognitiveservices.azure.com/.default").AuthenticateAsync(request, ct);
            request.Content = JsonContent.Create(new { input = text });
            return request;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var vector = json.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray()
            .Select(item => item.GetSingle()).ToArray();
        if (vector.Length == 0) throw new InvalidOperationException("Azure OpenAI returned an empty embedding.");
        return vector;
    }
}
