using EnterpriseRag;
using EnterpriseRag.Api;

var builder = WebApplication.CreateBuilder(args);
var useCloud = string.Equals(Environment.GetEnvironmentVariable("RAG_BACKEND"), "azure", StringComparison.OrdinalIgnoreCase);
builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddHealthChecks();
if (useCloud)
{
    builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(60) });
    builder.Services.AddSingleton<IVectorIndex>(sp =>
        AzureAiSearchVectorIndex.FromEnvironment(sp.GetRequiredService<HttpClient>()));
    builder.Services.AddSingleton<IEmbeddingGenerator>(sp =>
        AzureOpenAiEmbeddingGenerator.FromEnvironment(sp.GetRequiredService<HttpClient>()));
    builder.Services.AddSingleton<IGroundedAnswerGenerator>(sp =>
        AzureOpenAiAnswerGenerator.FromEnvironment(sp.GetRequiredService<HttpClient>()));
    builder.Services.AddSingleton<CloudRagService>();
}
var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/documents", (DocumentStore store) =>
    useCloud ? Results.StatusCode(501) : Results.Ok(store.List()));
app.MapPost("/documents", async (DocumentInput input, DocumentStore store, IServiceProvider services, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Id) || string.IsNullOrWhiteSpace(input.Text))
        return Results.BadRequest(new { error = "id and text are required" });
    if (input.Text.Length > 1_000_000 || input.Id.Length > 200)
        return Results.BadRequest(new { error = "document exceeds size limit" });
    var count = useCloud
        ? await services.GetRequiredService<CloudRagService>().UpsertAsync(input.Id, input.Text, ct)
        : store.Upsert(input.Id, input.Text);
    return Results.Ok(new { documentId = input.Id, chunkCount = count });
});
app.MapDelete("/documents/{id}", async (string id, DocumentStore store, IServiceProvider services, CancellationToken ct) =>
{
    if (!useCloud) return store.Delete(id) ? Results.NoContent() : Results.NotFound();
    await services.GetRequiredService<CloudRagService>().DeleteAsync(id, ct);
    return Results.NoContent();
});
app.MapPost("/ask", async (AskInput input, DocumentStore store, IServiceProvider services, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Question) || input.TopK is < 1 or > 20)
        return Results.BadRequest(new { error = "question required; topK must be between 1 and 20" });
    var answer = useCloud
        ? await services.GetRequiredService<CloudRagService>().AskAsync(input.Question, input.TopK, ct)
        : store.Ask(input.Question, input.TopK);
    return Results.Ok(answer);
});
app.Run();

public sealed record DocumentInput(string Id, string Text);
public sealed record AskInput(string Question, int TopK = 3);
