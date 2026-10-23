using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public class RagEngineTests
{
    [Fact]
    public void ChunkingPreservesOverlap()
    {
        var chunks = RagEngine.ChunkDocument("doc", "one two three four five six", 4, 2);
        Assert.Equal(2, chunks.Count);
        Assert.Equal("one two three four", chunks[0].Text);
        Assert.Equal("three four five six", chunks[1].Text);
    }

    [Fact]
    public void SearchReturnsRelevantDocument()
    {
        var chunks = new[] { new Chunk("azure", 0, "Azure Cosmos database capacity planning"),
            new Chunk("cooking", 0, "Baking bread with flour") };
        var hits = RagEngine.Search(chunks, "cosmos database");
        Assert.Single(hits);
        Assert.Equal("azure", hits[0].DocumentId);
    }

    [Fact]
    public void MissingEvidenceAbstains()
    {
        var chunks = new[] { new Chunk("doc", 0, "Azure Cosmos database") };
        var answer = RagEngine.Answer(chunks, "pineapple");
        Assert.Empty(answer.Sources);
        Assert.Contains("couldn't find", answer.Answer);
    }

    [Fact]
    public void InvalidOverlapThrows() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RagEngine.ChunkDocument("doc", "words", 3, 3));
}
