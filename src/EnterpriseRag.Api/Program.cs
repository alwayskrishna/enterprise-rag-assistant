using EnterpriseRag;
using EnterpriseRag.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var useCloud = string.Equals(Environment.GetEnvironmentVariable("RAG_BACKEND"), "azure", StringComparison.OrdinalIgnoreCase);
builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddHealthChecks();
if (useCloud)
{
    var tenant = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
    var audience = Environment.GetEnvironmentVariable("AZURE_API_AUDIENCE");
    var indexPrefix = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX");
    if (!Guid.TryParse(tenant, out var tenantGuid) || tenantGuid == Guid.Empty
        || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(indexPrefix))
        throw new InvalidOperationException("Cloud API requires AZURE_TENANT_ID, AZURE_API_AUDIENCE and AZURE_SEARCH_INDEX.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantGuid:D}/v2.0";
        options.Audience = audience;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://login.microsoftonline.com/{tenantGuid:D}/v2.0",
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
    builder.Services.AddAuthorization();
    builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(60) });
    builder.Services.AddSingleton<IEmbeddingGenerator>(sp =>
        AzureOpenAiEmbeddingGenerator.FromEnvironment(sp.GetRequiredService<HttpClient>()));
    builder.Services.AddSingleton<IGroundedAnswerGenerator>(sp =>
        AzureOpenAiAnswerGenerator.FromEnvironment(sp.GetRequiredService<HttpClient>()));
}
var app = builder.Build();
if (useCloud)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

CloudRagService CloudService(HttpContext context)
{
    var tenantId = TenantSecurity.GetTenantId(context.User);
    var indexPrefix = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX")!;
    var indexName = TenantSecurity.IndexName(indexPrefix, tenantId);
    var httpClient = context.RequestServices.GetRequiredService<HttpClient>();
    var index = AzureAiSearchVectorIndex.FromEnvironment(httpClient, indexName);
    return new CloudRagService(index,
        context.RequestServices.GetRequiredService<IEmbeddingGenerator>(),
        context.RequestServices.GetRequiredService<IGroundedAnswerGenerator>());
}

app.MapHealthChecks("/health");
var list = app.MapGet("/documents", (DocumentStore store) =>
    useCloud ? Results.StatusCode(501) : Results.Ok(store.List()));
var upsert = app.MapPost("/documents", async (DocumentInput input, DocumentStore store, HttpContext context, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Id) || string.IsNullOrWhiteSpace(input.Text))
        return Results.BadRequest(new { error = "id and text are required" });
    if (input.Text.Length > 1_000_000 || input.Id.Length > 200)
        return Results.BadRequest(new { error = "document exceeds size limit" });
    var count = useCloud
        ? await CloudService(context).UpsertAsync(input.Id, input.Text, ct)
        : store.Upsert(input.Id, input.Text);
    return Results.Ok(new { documentId = input.Id, chunkCount = count });
});
var delete = app.MapDelete("/documents/{id}", async (string id, DocumentStore store, HttpContext context, CancellationToken ct) =>
{
    if (!useCloud) return store.Delete(id) ? Results.NoContent() : Results.NotFound();
    await CloudService(context).DeleteAsync(id, ct);
    return Results.NoContent();
});
var ask = app.MapPost("/ask", async (AskInput input, DocumentStore store, HttpContext context, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Question) || input.TopK is < 1 or > 20)
        return Results.BadRequest(new { error = "question required; topK must be between 1 and 20" });
    var answer = useCloud
        ? await CloudService(context).AskAsync(input.Question, input.TopK, ct)
        : store.Ask(input.Question, input.TopK);
    return Results.Ok(answer);
});
if (useCloud)
{
    list.RequireAuthorization();
    upsert.RequireAuthorization();
    delete.RequireAuthorization();
    ask.RequireAuthorization();
}
app.Run();

public sealed record DocumentInput(string Id, string Text);
public sealed record AskInput(string Question, int TopK = 3);
