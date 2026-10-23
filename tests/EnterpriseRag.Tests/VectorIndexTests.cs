using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class VectorIndexTests
{
    private sealed class FakeEmbeddings : IEmbeddingGenerator
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(text.Contains("queue", StringComparison.OrdinalIgnoreCase)
                ? new[] { 1f, 0f } : new[] { 0f, 1f });
    }

    [Fact]
    public async Task PersistsIndexAcrossInstances()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var index = new FileVectorIndex(path);
            await VectorIndexing.IndexDocumentAsync(index, new FakeEmbeddings(), "runbook", "Inspect queue depth.");
            var reopened = new FileVectorIndex(path);
            var results = await reopened.SearchAsync(new[] { 1f, 0f });
            Assert.Single(results);
            Assert.Equal("runbook", results[0].DocumentId);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task UpsertReplacesExistingDocumentAndDeleteRemovesIt()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var index = new FileVectorIndex(path);
            await index.UpsertDocumentAsync("doc", new[] { new IndexedChunk("doc", 0, "old", new[] { 1f, 0f }) });
            await index.UpsertDocumentAsync("doc", new[] { new IndexedChunk("doc", 0, "new", new[] { 0f, 1f }) });
            Assert.Empty(await index.SearchAsync(new[] { 1f, 0f }));
            Assert.Equal("new", Assert.Single(await index.SearchAsync(new[] { 0f, 1f })).Text);
            await index.DeleteDocumentAsync("doc");
            Assert.Empty(await index.SearchAsync(new[] { 0f, 1f }));
        }
        finally { File.Delete(path); }
    }
}
