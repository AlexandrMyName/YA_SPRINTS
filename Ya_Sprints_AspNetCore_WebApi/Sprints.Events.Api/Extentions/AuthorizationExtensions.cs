using Microsoft.AspNetCore.Authorization; 
using Sprints.Events.Presentation.Extentions;


namespace Sprints.Events.Presentation.Extensions;


public static class AuthorizationExtensions
{

    public static IServiceCollection AddAppAuthorization(
        this IServiceCollection services, IConfiguration config)
    {

        var mode = config["Jwt:Mode"] ?? "HS256";
        var isHs256 = string.Equals(mode, "HS256", StringComparison.OrdinalIgnoreCase);

        var schemes = isHs256
            ? new[] { AuthSchemes.HS256 }
            : new[] { AuthSchemes.RS256 };


        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemes)
                .Build();

            options.AddPolicy(AuthPolicies.AnyAuthenticated, p => p
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(schemes));

            options.AddPolicy(AuthPolicies.Admin, p => p
                .RequireAuthenticatedUser()
                .RequireRole("Admin")
                .AddAuthenticationSchemes(schemes));
        });

        return services;
    }
}