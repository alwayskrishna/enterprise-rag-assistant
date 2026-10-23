using System.Text.Json;

namespace EnterpriseRag;

public sealed record IndexedChunk(string DocumentId, int ChunkIndex, string Text, float[] Vector);

public interface IVectorIndex
{
    Task UpsertDocumentAsync(string documentId, IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchHit>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default);
}

public sealed class FileVectorIndex(string path) : IVectorIndex
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string path = Path.GetFullPath(path);

    public async Task UpsertDocumentAsync(string documentId, IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentException("Document ID required.", nameof(documentId));
        if (chunks.Any(c => c.DocumentId != documentId || c.Vector.Length == 0))
            throw new ArgumentException("All chunks must have the same document ID and nonempty vectors.", nameof(chunks));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var all = await ReadAsync(cancellationToken);
            all.RemoveAll(c => c.DocumentId == documentId);
            all.AddRange(chunks);
            await WriteAsync(all, cancellationToken);
        }
        finally { gate.Release(); }
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var all = await ReadAsync(cancellationToken);
            all.RemoveAll(c => c.DocumentId == documentId);
            await WriteAsync(all, cancellationToken);
        }
        finally { gate.Release(); }
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default)
    {
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var all = await ReadAsync(cancellationToken);
            return all.Select(c => new SearchHit(c.DocumentId, c.ChunkIndex, c.Text,
                    SemanticSearch.CosineSimilarity(queryVector, c.Vector)))
                .Where(hit => hit.Score > 0)
                .OrderByDescending(hit => hit.Score)
                .ThenBy(hit => hit.DocumentId, StringComparer.Ordinal)
                .ThenBy(hit => hit.ChunkIndex).Take(topK).ToArray();
        }
        finally { gate.Release(); }
    }

    private async Task<List<IndexedChunk>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return new List<IndexedChunk>();
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<IndexedChunk>>(stream, cancellationToken: cancellationToken)
            ?? new List<IndexedChunk>();
    }

    private async Task WriteAsync(List<IndexedChunk> chunks, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = File.Create(temporary))
                await JsonSerializer.SerializeAsync(stream, chunks, cancellationToken: cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public static class VectorIndexing
{
    public static async Task IndexDocumentAsync(IVectorIndex index, IEmbeddingGenerator generator,
        string documentId, string text, CancellationToken cancellationToken = default)
    {
        var chunks = RagEngine.ChunkDocument(documentId, text);
        var vectors = new List<IndexedChunk>();
        foreach (var chunk in chunks)
            vectors.Add(new IndexedChunk(chunk.DocumentId, chunk.Index, chunk.Text,
                await generator.EmbedAsync(chunk.Text, cancellationToken)));
        await index.UpsertDocumentAsync(documentId, vectors, cancellationToken);
    }
}
