using Microsoft.AspNetCore.Authorization;


namespace SprintASP_NetCore_API.Presentation.Extentions;


public static class AuthorizationExtensions
{

    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {

        services.AddAuthorization(options =>
        {

            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthSchemes.RS256, AuthSchemes.HS256)
                .Build();

            options.AddPolicy(AuthPolicies.AnyAuthenticated, p => p
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthSchemes.RS256, AuthSchemes.HS256));

            options.AddPolicy(AuthPolicies.Admin, p => p
                .RequireAuthenticatedUser()
                .RequireRole("Admin")
                .AddAuthenticationSchemes(AuthSchemes.RS256, AuthSchemes.HS256));

            options.AddPolicy(AuthPolicies.AdminRs256Only, p => p
                .RequireAuthenticatedUser()
                .RequireRole("Admin")
                .AddAuthenticationSchemes(AuthSchemes.RS256));
        });

        return services;
    }
}
