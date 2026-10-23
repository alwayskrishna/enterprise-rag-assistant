namespace EnterpriseRag;

/// <summary>Thread-safe in-memory document store for the offline API demo.</summary>
public sealed class DocumentStore
{
    private readonly Dictionary<string, IReadOnlyList<Chunk>> documents = new(StringComparer.Ordinal);
    private readonly object gate = new();

    public int Upsert(string id, string text)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document ID required.", nameof(id));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Document text required.", nameof(text));
        var chunks = RagEngine.ChunkDocument(id, text);
        lock (gate) documents[id] = chunks;
        return chunks.Count;
    }

    public bool Delete(string id)
    {
        lock (gate) return documents.Remove(id);
    }

    public IReadOnlyList<string> List()
    {
        lock (gate) return documents.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    public RagAnswer Ask(string question, int topK = 3)
    {
        if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("Question required.", nameof(question));
        if (topK < 1 || topK > 20) throw new ArgumentOutOfRangeException(nameof(topK));
        Chunk[] snapshot;
        lock (gate) snapshot = documents.Values.SelectMany(x => x).ToArray();
        return RagEngine.Answer(snapshot, question, topK);
    }
}
