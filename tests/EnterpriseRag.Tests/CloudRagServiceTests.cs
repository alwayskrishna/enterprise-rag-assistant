using EnterpriseRag;
using EnterpriseRag.Api;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class CloudRagServiceTests
{
    private sealed class FakeEmbeddings : IEmbeddingGenerator
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(new float[] { 1, 0 });
    }

    private sealed class FakeGenerator : IGroundedAnswerGenerator
    {
        public Task<string> GenerateAsync(string question, IReadOnlyList<SearchHit> sources, CancellationToken cancellationToken = default) =>
            Task.FromResult("Grounded answer [1]");
    }

    private sealed class FakeIndex : IVectorIndex
    {
        private readonly Dictionary<string, IReadOnlyList<IndexedChunk>> docs = new();
        public Task UpsertDocumentAsync(string documentId, IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken = default)
        {
            docs[documentId] = chunks;
            return Task.CompletedTask;
        }
        public Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            docs.Remove(documentId);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<SearchHit>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SearchHit>>(docs.Values.SelectMany(x => x)
                .Take(topK).Select(x => new SearchHit(x.DocumentId, x.ChunkIndex, x.Text, 1)).ToArray());
    }

    [Fact]
    public async Task IndexAskAndDelete()
    {
        var service = new CloudRagService(new FakeIndex(), new FakeEmbeddings(), new FakeGenerator());
        Assert.Equal(1, await service.UpsertAsync("runbook", "Queue latency recovery"));
        var answer = await service.AskAsync("latency", 3);
        Assert.Equal("Grounded answer [1]", answer.Answer);
        Assert.Equal("runbook", Assert.Single(answer.Sources).DocumentId);
        await service.DeleteAsync("runbook");
        Assert.Empty((await service.AskAsync("latency", 3)).Sources);
    }
}
