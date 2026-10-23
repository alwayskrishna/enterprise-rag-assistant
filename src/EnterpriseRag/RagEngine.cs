using System.Text.RegularExpressions;

namespace EnterpriseRag;

public sealed record Chunk(string DocumentId, int Index, string Text);
public sealed record SearchHit(string DocumentId, int ChunkIndex, string Text, double Score);
public sealed record RagAnswer(string Answer, IReadOnlyList<SearchHit> Sources);

public static class RagEngine
{
    public static IReadOnlyList<Chunk> ChunkDocument(string documentId, string text, int wordsPerChunk = 80, int overlap = 15)
    {
        if (string.IsNullOrWhiteSpace(documentId)) throw new ArgumentException("Document ID is required.", nameof(documentId));
        if (wordsPerChunk <= 0 || overlap < 0 || overlap >= wordsPerChunk)
            throw new ArgumentOutOfRangeException(nameof(overlap), "Chunk size must be positive and overlap smaller than chunk size.");
        var words = Regex.Split(text.Trim(), @"\s+").Where(w => w.Length > 0).ToArray();
        var chunks = new List<Chunk>();
        for (int start = 0; start < words.Length; start += wordsPerChunk - overlap)
        {
            chunks.Add(new Chunk(documentId, chunks.Count, string.Join(" ", words.Skip(start).Take(wordsPerChunk))));
            if (start + wordsPerChunk >= words.Length) break;
        }
        return chunks;
    }

    public static Dictionary<string, double> Embed(string text)
    {
        var tokens = Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}]+")
            .Select(m => m.Value).Where(t => t.Length > 1).ToArray();
        if (tokens.Length == 0) return new Dictionary<string, double>();
        var counts = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => (double)g.Count());
        var norm = Math.Sqrt(counts.Values.Sum(v => v * v));
        return counts.ToDictionary(kv => kv.Key, kv => kv.Value / norm);
    }

    public static double CosineSimilarity(Dictionary<string, double> a, Dictionary<string, double> b) =>
        a.Sum(kv => kv.Value * b.GetValueOrDefault(kv.Key));

    public static IReadOnlyList<SearchHit> Search(IEnumerable<Chunk> chunks, string query, int topK = 3)
    {
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK));
        var vector = Embed(query);
        return chunks.Select(c => new SearchHit(c.DocumentId, c.Index, c.Text, CosineSimilarity(vector, Embed(c.Text))))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score).ThenBy(hit => hit.DocumentId, StringComparer.Ordinal)
            .ThenBy(hit => hit.ChunkIndex).Take(topK).ToArray();
    }

    public static RagAnswer Answer(IEnumerable<Chunk> chunks, string query, int topK = 3)
    {
        var hits = Search(chunks, query, topK);
        if (hits.Count == 0) return new RagAnswer("I couldn't find supporting information in the indexed documents.", hits);
        var evidence = string.Join(" ", hits.Select((h, i) => $"[{i + 1}] {h.Text}"));
        return new RagAnswer($"Relevant excerpts (not an AI-generated synthesis): {evidence}", hits);
    }
}
