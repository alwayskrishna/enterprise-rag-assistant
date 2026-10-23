using EnterpriseRag;

namespace EnterpriseRag.Api;

/// <summary>Azure-backed ingestion and grounded question answering, with dependencies injectable for tests.</summary>
public sealed class CloudRagService(IVectorIndex index, IEmbeddingGenerator embeddings, IGroundedAnswerGenerator generator)
{
    public async Task<int> UpsertAsync(string documentId, string text, CancellationToken cancellationToken = default)
    {
        var chunks = RagEngine.ChunkDocument(documentId, text);
        await VectorIndexing.IndexDocumentAsync(index, embeddings, documentId, text, cancellationToken);
        return chunks.Count;
    }

    public Task DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
        index.DeleteDocumentAsync(documentId, cancellationToken);

    public async Task<RagAnswer> AskAsync(string question, int topK, CancellationToken cancellationToken = default)
    {
        var vector = await embeddings.EmbedAsync(question, cancellationToken);
        var hits = await index.SearchAsync(vector, topK, cancellationToken);
        return await new GroundedAnswerService(generator).AnswerFromSourcesAsync(hits, question, cancellationToken);
    }
}
