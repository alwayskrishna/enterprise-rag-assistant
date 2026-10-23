using System.Net.Http.Json;
using System.Text.Json;

namespace EnterpriseRag;

/// <summary>
/// Azure AI Search adapter for an existing index with key 'id', filterable 'documentId',
/// integer 'chunkIndex', searchable 'text', and vector 'embedding' fields.
/// Provision the index and vector profile before using this adapter.
/// </summary>
public sealed class AzureAiSearchVectorIndex(HttpClient client, Uri endpoint, string indexName, string apiKey) : IVectorIndex
{
    private const string ApiVersion = "2024-07-01";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static AzureAiSearchVectorIndex FromEnvironment(HttpClient client)
    {
        var raw = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
        var name = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX");
        var key = Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY");
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !endpoint.Host.EndsWith(".search.windows.net", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Set AZURE_SEARCH_ENDPOINT, AZURE_SEARCH_INDEX and AZURE_SEARCH_API_KEY.");
        return new AzureAiSearchVectorIndex(client, endpoint, name, key);
    }

    public async Task UpsertDocumentAsync(string documentId, IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentException("Document ID required.", nameof(documentId));
        if (chunks.Any(c => c.DocumentId != documentId || c.Vector.Length == 0))
            throw new ArgumentException("Invalid chunk document ID or embedding.", nameof(chunks));
        // Replace semantics: remove prior chunks before upload so stale chunks cannot remain.
        await DeleteDocumentAsync(documentId, cancellationToken);
        foreach (var batch in chunks.Chunk(500))
        {
            var records = batch.Select(c => new
            {
                @search_action = "upload",
                id = KeyFor(c.DocumentId, c.ChunkIndex),
                documentId = c.DocumentId,
                chunkIndex = c.ChunkIndex,
                text = c.Text,
                embedding = c.Vector
            }).Select(c => new Dictionary<string, object>
            {
                ["@search.action"] = c.search_action,
                ["id"] = c.id,
                ["documentId"] = c.documentId,
                ["chunkIndex"] = c.chunkIndex,
                ["text"] = c.text,
                ["embedding"] = c.embedding
            }).ToArray();
            using var uploadResponse = await SendAsync(HttpMethod.Post, "docs/index", new { value = records }, cancellationToken);
        }
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentException("Document ID required.", nameof(documentId));
        // Search for existing chunk keys, then delete by key. Paged to avoid silently missing large documents.
        // Bound pagination to avoid an endless loop if deleted keys remain visible during indexing lag.
        for (var page = 0; page < 20; page++)
        {
            using var response = await SendAsync(HttpMethod.Post, "docs/search",
                new { search = "*", filter = $"documentId eq '{EscapeFilter(documentId)}'", select = "id", top = 500 },
                cancellationToken);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var ids = json.RootElement.GetProperty("value").EnumerateArray()
                .Select(v => v.GetProperty("id").GetString()!).ToArray();
            if (ids.Length == 0) break;
            var deletes = ids.Select(id => new Dictionary<string, object> { ["@search.action"] = "delete", ["id"] = id }).ToArray();
            using var deleteResponse = await SendAsync(HttpMethod.Post, "docs/index", new { value = deletes }, cancellationToken);
            if (ids.Length < 500) break;
        }
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default)
    {
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK));
        if (queryVector.Length == 0) throw new ArgumentException("Query embedding must not be empty.", nameof(queryVector));
        using var response = await SendAsync(HttpMethod.Post, "docs/search", new
        {
            search = "*",
            vectorQueries = new[] { new { kind = "vector", vector = queryVector, fields = "embedding", k = topK } },
            select = "documentId,chunkIndex,text",
            top = topK
        }, cancellationToken);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return json.RootElement.GetProperty("value").EnumerateArray()
            .Select(v => new SearchHit(
                v.GetProperty("documentId").GetString()!,
                v.GetProperty("chunkIndex").GetInt32(),
                v.GetProperty("text").GetString()!,
                v.GetProperty("@search.score").GetDouble())).ToArray();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        var uri = new Uri(endpoint, $"indexes/{Uri.EscapeDataString(indexName)}/{path}?api-version={ApiVersion}");
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException($"Azure AI Search request failed with HTTP {(int)status}.", null, status);
        }
        return response;
    }

    private static string EscapeFilter(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static string KeyFor(string documentId, int chunkIndex) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(documentId + ":" + chunkIndex))).ToLowerInvariant();
}
