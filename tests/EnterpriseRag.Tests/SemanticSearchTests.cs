using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public class SemanticSearchTests
{
    private sealed class FakeEmbeddings : IEmbeddingGenerator
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(text switch
            {
                "how to handle latency" => new[] { 1f, 0f },
                "inspect slow message processing" => new[] { 0.9f, 0.1f },
                "bake sourdough bread" => new[] { 0f, 1f },
                _ => new[] { 0f, 0f }
            });
    }

    [Fact]
    public async Task RanksSemanticMatchAboveUnrelatedText()
    {
        var chunks = new[]
        {
            new Chunk("bread", 0, "bake sourdough bread"),
            new Chunk("runbook", 0, "inspect slow message processing")
        };
        var results = await SemanticSearch.SearchAsync(chunks, "how to handle latency", new FakeEmbeddings());
        Assert.Equal(2, results.Count);
        Assert.Equal("runbook", results[0].DocumentId);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void RejectsDimensionMismatch() =>
        Assert.Throws<ArgumentException>(() => SemanticSearch.CosineSimilarity(new float[] { 1, 2 }, new float[] { 1 }));

    [Fact]
    public void ZeroVectorReturnsZero() =>
        Assert.Equal(0, SemanticSearch.CosineSimilarity(new float[] { 0, 0 }, new float[] { 1, 2 }));

    [Fact]
    public async Task EmptyIndexAvoidsModelCall()
    {
        var results = await SemanticSearch.SearchAsync(Array.Empty<Chunk>(), "query", new FakeEmbeddings());
        Assert.Empty(results);
    }
}
