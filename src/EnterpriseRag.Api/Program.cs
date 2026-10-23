using EnterpriseRag;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/documents", (DocumentStore store) => Results.Ok(store.List()));
app.MapPost("/documents", (DocumentInput input, DocumentStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Id) || string.IsNullOrWhiteSpace(input.Text))
        return Results.BadRequest(new { error = "id and text are required" });
    if (input.Text.Length > 1_000_000 || input.Id.Length > 200)
        return Results.BadRequest(new { error = "document exceeds size limit" });
    var count = store.Upsert(input.Id, input.Text);
    return Results.Ok(new { documentId = input.Id, chunkCount = count });
});
app.MapDelete("/documents/{id}", (string id, DocumentStore store) =>
    store.Delete(id) ? Results.NoContent() : Results.NotFound());
app.MapPost("/ask", (AskInput input, DocumentStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Question) || input.TopK is < 1 or > 20)
        return Results.BadRequest(new { error = "question required; topK must be between 1 and 20" });
    return Results.Ok(store.Ask(input.Question, input.TopK));
});
app.Run();

public sealed record DocumentInput(string Id, string Text);
public sealed record AskInput(string Question, int TopK = 3);
