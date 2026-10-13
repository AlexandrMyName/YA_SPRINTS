using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace SprintASP_NetCore_API.Presentation.Extentions;


public static class JwtBearerExtensions
{

    public static void UseMultiSourceToken(this JwtBearerEvents events)
    {

        events.OnMessageReceived = ctx =>
        {

            var auth = ctx.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(auth) &&
                auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            var xAccess = ctx.Request.Headers["X-Access-Token"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(xAccess))
            {
                ctx.Token = xAccess;
                return Task.CompletedTask;
            }

            var cookieToken = ctx.Request.Cookies["jwt"];
            if (!string.IsNullOrWhiteSpace(cookieToken))
                ctx.Token = cookieToken;

            return Task.CompletedTask;
        };
    }
}
