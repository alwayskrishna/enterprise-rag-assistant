using EnterpriseRag;

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
    chunks.AddRange(RagEngine.ChunkDocument(Path.GetFileName(path), await File.ReadAllTextAsync(path)));
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
