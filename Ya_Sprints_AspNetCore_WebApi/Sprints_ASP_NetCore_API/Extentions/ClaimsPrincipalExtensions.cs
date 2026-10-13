using System.Security.Claims;


namespace SprintASP_NetCore_API.Presentation.Extentions;


public static class ClaimsPrincipalExtensions
{

    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst("userId")?.Value
               ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(raw) || !Guid.TryParse(raw, out var id))
            throw new UnauthorizedAccessException("В токене нет userId");

        return id;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}
