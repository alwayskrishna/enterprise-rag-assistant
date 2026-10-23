using System.Text.Json;

namespace EnterpriseRag;

public sealed record RetrievalCase(string Id, string Query, string ExpectedDocumentId);
public sealed record RetrievalCaseResult(string Id, bool Passed, string? TopDocumentId);
public sealed record RetrievalReport(int Total, int Passed, double AccuracyAtOne, IReadOnlyList<RetrievalCaseResult> Cases);

public static class RetrievalEvaluation
{
    public static RetrievalReport Run(IEnumerable<Chunk> chunks, IEnumerable<RetrievalCase> cases)
    {
        var corpus = chunks.ToArray();
        var results = cases.Select(test =>
        {
            var top = RagEngine.Search(corpus, test.Query, 1).FirstOrDefault();
            return new RetrievalCaseResult(test.Id,
                string.Equals(top?.DocumentId, test.ExpectedDocumentId, StringComparison.Ordinal),
                top?.DocumentId);
        }).ToArray();
        return new RetrievalReport(results.Length, results.Count(r => r.Passed),
            results.Length == 0 ? 0 : (double)results.Count(r => r.Passed) / results.Length, results);
    }

    public static async Task<IReadOnlyList<RetrievalCase>> LoadCasesAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<RetrievalCase>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
            ?? throw new InvalidDataException("Evaluation file is empty.");
    }
}
