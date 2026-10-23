using System.Security.Claims;
using EnterpriseRag.Api;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class ApiSecurityTests
{
    [Fact]
    public void RequiresAuthenticatedIdentityAndExactScope()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("scp", "Rag.Read Rag.Write")], "test"));
        Assert.True(ApiSecurity.HasPermission(user, "Rag.Read"));
        Assert.True(ApiSecurity.HasPermission(user, "Rag.Write"));
        Assert.False(ApiSecurity.HasPermission(user, "Rag.Admin"));
        Assert.False(ApiSecurity.HasPermission(new ClaimsPrincipal(), "Rag.Read"));
    }

    [Fact]
    public void SupportsAppRoles()
    {
        var app = new ClaimsPrincipal(new ClaimsIdentity([new Claim("roles", "Rag.Write")], "test"));
        Assert.True(ApiSecurity.HasPermission(app, "Rag.Write"));
    }

    [Theory]
    [InlineData("safe-doc_1", true)]
    [InlineData("../other", false)]
    [InlineData("space id", false)]
    [InlineData("", false)]
    public void DocumentIdsAreConstrained(string id, bool valid) =>
        Assert.Equal(valid, ApiSecurity.ValidDocumentId(id));

    [Fact]
    public void LongQuestionsAreRejected() =>
        Assert.False(ApiSecurity.ValidQuestion(new string('q', 4097)));
}
