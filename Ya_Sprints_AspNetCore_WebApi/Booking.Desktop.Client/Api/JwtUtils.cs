using System.Text.Json;
using System.Text;
using System;


namespace Booking.Desktop.Client.Api;


public static class JwtUtils
{

    public static bool IsAdmin(string accessToken)
    {
        var role = GetClaim(accessToken, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                   ?? GetClaim(accessToken, "role");
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }


    public static string? GetClaim(string token, string claimType)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var s = parts[1].Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(s));

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(claimType, out var v)
                ? v.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
