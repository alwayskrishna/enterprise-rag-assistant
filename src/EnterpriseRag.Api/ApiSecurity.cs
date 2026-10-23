using System.Security.Claims;

namespace EnterpriseRag.Api;

public static class ApiSecurity
{
    public static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        var scopes = user.FindFirst("scp")?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (scopes.Contains(permission, StringComparer.Ordinal)) return true;
        return user.FindAll("roles").Any(role => role.Value == permission);
    }

    public static bool ValidDocumentId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= 200 &&
        id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    public static bool ValidQuestion(string? question) =>
        !string.IsNullOrWhiteSpace(question) && question.Length <= 4096;
}
