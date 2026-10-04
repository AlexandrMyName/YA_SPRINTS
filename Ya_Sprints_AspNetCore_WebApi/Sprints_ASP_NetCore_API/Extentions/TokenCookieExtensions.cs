

namespace SprintASP_NetCore_API.Presentation.Extentions;


public static class TokenCookieExtensions
{

    public const string AccessCookieName = "jwt";
    public const string RefreshCookieName = "refreshToken";

    public static void SetAccessTokenCookie(this HttpResponse r, string token, int expiresInSeconds)
        => r.Cookies.Append(AccessCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds),
            Path = "/api"
        });

    public static void SetRefreshTokenCookie(this HttpResponse r, string token, int expiresInDays)
        => r.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(expiresInDays),
            Path = "/api/v1/auth"
        });

    public static void ClearAuthCookies(this HttpResponse r)
    {
        r.Cookies.Delete(AccessCookieName, new CookieOptions { Path = "/api" });
        r.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/api/v1/auth" });
    }
}
