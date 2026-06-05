using System.Text;
using System.Text.Json;

namespace Dashboards_reports.CollectionTracker.Services;

public static class UserContext
{
    public static string? GetUserId(HttpContext? httpContext)
    {
        var token = GetBearerToken(httpContext);
        if (token is null) return null;

        var claims = DecodeJwtPayload(token);
        if (claims is null) return null;

        // Try EmployeeID first (matches the frontend JWT structure), then standard claims
        if (claims.TryGetValue("EmployeeID", out var employeeId) && !string.IsNullOrWhiteSpace(employeeId))
            return employeeId;

        if (claims.TryGetValue("sub", out var sub) && !string.IsNullOrWhiteSpace(sub))
            return sub;

        if (claims.TryGetValue("nameid", out var nameId) && !string.IsNullOrWhiteSpace(nameId))
            return nameId;

        return null;
    }

    public static string? GetUserDisplayName(HttpContext? httpContext)
    {
        var token = GetBearerToken(httpContext);
        if (token is null) return null;

        var claims = DecodeJwtPayload(token);
        if (claims is null) return null;

        var firstName = claims.GetValueOrDefault("FirstName") ?? "";
        var lastName = claims.GetValueOrDefault("LastName") ?? "";
        var fullName = $"{firstName} {lastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private static string? GetBearerToken(HttpContext? httpContext)
    {
        var header = httpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (header is null || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return header["Bearer ".Length..].Trim();
    }

    private static Dictionary<string, string>? DecodeJwtPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            // Base64url → Base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }
}
