using EnterpriseRag;

if (args.Length < 2 || args[0] != "ask")
{
    Console.Error.WriteLine("Usage: dotnet run --project src/EnterpriseRag -- ask \"question\" [document.txt ...] [--ai]");
    return 2;
}

var query = args[1];
var useAi = args.Contains("--ai", StringComparer.Ordinal);
var paths = args.Skip(2).Where(arg => arg != "--ai").DefaultIfEmpty("samples/operations.txt");
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

RagAnswer answer;
if (useAi)
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var generator = AzureOpenAiAnswerGenerator.FromEnvironment(client);
    answer = await new GroundedAnswerService(generator).AnswerAsync(chunks, query);
}
else
{
    answer = RagEngine.Answer(chunks, query);
}

Console.WriteLine(answer.Answer);
foreach (var source in answer.Sources)
    Console.WriteLine($"Source: {source.DocumentId} chunk={source.ChunkIndex} score={source.Score:F3}");
return 0;
