using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseRag.Api;

public static class TenantSecurity
{
    public static string GetTenantId(ClaimsPrincipal user)
    {
        var tenantId = user.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tenantId, out var tenant) || tenant == Guid.Empty)
            throw new UnauthorizedAccessException("A valid Microsoft Entra tenant ID is required.");
        return tenant.ToString("D");
    }

    public static string IndexName(string baseIndexName, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(baseIndexName) || baseIndexName.Length > 90)
            throw new ArgumentException("Invalid index prefix.", nameof(baseIndexName));
        if (!Guid.TryParse(tenantId, out var tenant) || tenant == Guid.Empty)
            throw new ArgumentException("Valid tenant ID required.", nameof(tenantId));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tenant.ToString("D"))))
            .ToLowerInvariant()[..16];
        return $"{baseIndexName}-{hash}";
    }
}
