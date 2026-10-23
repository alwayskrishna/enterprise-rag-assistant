using EnterpriseRag;

var reader = new DocumentReader();
if (args.Length > 0 && args[0] == "eval")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: eval <cases.json> <document.txt|document.pdf> [...]");
        return 2;
    }
    var evalChunks = new List<Chunk>();
    foreach (var path in args.Skip(2))
        evalChunks.AddRange(RagEngine.ChunkDocument(Path.GetFileName(path), await reader.ReadAsync(path)));
    var cases = await RetrievalEvaluation.LoadCasesAsync(args[1]);
    var report = RetrievalEvaluation.Run(evalChunks, cases);
    Console.WriteLine($"Retrieval accuracy@1: {report.Passed}/{report.Total} ({report.AccuracyAtOne:P1})");
    foreach (var item in report.Cases)
        Console.WriteLine($"{(item.Passed ? "PASS" : "FAIL")} {item.Id}: top={item.TopDocumentId ?? "(none)"}");
    return report.Total > 0 && report.Passed == report.Total ? 0 : 1;
}

if (args.Length > 0 && args[0] is "index" or "search" or "delete")
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: index <file> | search <question> | delete <document-id> [--index-file path] [--azure-search]");
        return 2;
    }
    var optionPosition = Array.IndexOf(args, "--index-file");
    if (optionPosition >= 0 && optionPosition + 1 >= args.Length)
    {
        Console.Error.WriteLine("--index-file requires a path.");
        return 2;
    }
    var indexPath = optionPosition >= 0 ? args[optionPosition + 1] : ".data/vectors.json";
    var useAzureSearch = args.Contains("--azure-search", StringComparer.Ordinal);
    using var searchClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    IVectorIndex index = useAzureSearch
        ? AzureAiSearchVectorIndex.FromEnvironment(searchClient)
        : new FileVectorIndex(indexPath);
    if (args[0] == "delete")
    {
        await index.DeleteDocumentAsync(args[1]);
        Console.WriteLine($"Deleted: {args[1]}");
        return 0;
    }
    using var embeddingClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    var embeddings = AzureOpenAiEmbeddingGenerator.FromEnvironment(embeddingClient);
    if (args[0] == "index")
    {
        if (!File.Exists(args[1]))
        {
            Console.Error.WriteLine($"Document not found: {args[1]}");
            return 2;
        }
        await VectorIndexing.IndexDocumentAsync(index, embeddings, Path.GetFileName(args[1]), await reader.ReadAsync(args[1]));
        Console.WriteLine($"Indexed: {args[1]}");
        return 0;
    }
    var queryVector = await embeddings.EmbedAsync(args[1]);
    var hits = await index.SearchAsync(queryVector);
    if (hits.Count == 0) Console.WriteLine("No matching indexed excerpts.");
    foreach (var hit in hits)
        Console.WriteLine($"{hit.DocumentId} chunk={hit.ChunkIndex} score={hit.Score:F3}: {hit.Text}");
    return 0;
}

if (args.Length < 2 || args[0] != "ask")
{
    Console.Error.WriteLine("Usage: dotnet run --project src/EnterpriseRag -- ask \"question\" [document.txt ...] [--semantic] [--ai]");
    return 2;
}

var query = args[1];
var useAi = args.Contains("--ai", StringComparer.Ordinal);
var useSemantic = args.Contains("--semantic", StringComparer.Ordinal);
var paths = args.Skip(2).Where(arg => arg != "--ai" && arg != "--semantic").DefaultIfEmpty("samples/operations.txt");
var chunks = new List<Chunk>();
foreach (var path in paths)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"Document not found: {path}");
        return 2;
    }
    chunks.AddRange(RagEngine.ChunkDocument(Path.GetFileName(path), await reader.ReadAsync(path)));
}

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
IReadOnlyList<SearchHit> sources = useSemantic
    ? await SemanticSearch.SearchAsync(chunks, query, AzureOpenAiEmbeddingGenerator.FromEnvironment(client))
    : RagEngine.Search(chunks, query);

RagAnswer answer;
if (useAi)
{
    var generator = AzureOpenAiAnswerGenerator.FromEnvironment(client);
    answer = await new GroundedAnswerService(generator).AnswerFromSourcesAsync(sources, query);
}
else if (sources.Count == 0)
{
    answer = new RagAnswer("I couldn't find supporting information in the indexed documents.", sources);
}
else
{
    answer = new RagAnswer(
        "Relevant excerpts (not an AI-generated synthesis): " +
        string.Join(" ", sources.Select((source, index) => $"[{index + 1}] {source.Text}")), sources);
}

Console.WriteLine(answer.Answer);
foreach (var source in answer.Sources)
    Console.WriteLine($"Source: {source.DocumentId} chunk={source.ChunkIndex} score={source.Score:F3}");
return 0;
