using System.Security.Claims;
using EnterpriseRag.Api;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class TenantSecurityTests
{
    [Fact]
    public void DifferentTenantsGetDifferentIndexes()
    {
        var a = TenantSecurity.IndexName("rag", "11111111-1111-1111-1111-111111111111");
        var b = TenantSecurity.IndexName("rag", "22222222-2222-2222-2222-222222222222");
        Assert.NotEqual(a, b);
        Assert.Equal(a, TenantSecurity.IndexName("rag", "11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void TenantComesFromClaimsNotRequestPayload()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim("tid", "11111111-1111-1111-1111-111111111111")
        }));
        Assert.Equal("11111111-1111-1111-1111-111111111111", TenantSecurity.GetTenantId(user));
        Assert.Throws<UnauthorizedAccessException>(() => TenantSecurity.GetTenantId(new ClaimsPrincipal()));
    }
}
