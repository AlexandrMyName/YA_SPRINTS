using Sprints.Events.Middlewares.Extentions.Configurations;
using Sprints.Events.Application.Abstractions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Sprints.Events.Presentation.Extentions;
using Sprints.Events.Presentation.Extensions;
using Sprints.Events.Infrastructure.Security;
using Sprints.Events.Infrastructure.DI;
using Microsoft.IdentityModel.Tokens;
using Sprints.Events.Application.DI;
using Sprints.Events.Middlewares;
using Microsoft.AspNetCore.Mvc;
using System.Text;

[assembly: ApiController]

namespace Sprints.Events.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddCorsPolicies()
            .AddControllersWithCacheAndValidation()
            .AddEndpointsApiExplorer()
            .AddSwaggerGenWithDocumentation()
            .AddApiVersioningCustom();

        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration);

        #region SECURITY

        builder.Services.Configure<JwtOptions>(
            builder.Configuration.GetSection(JwtOptions.SectionName));

        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

        var mode = jwtSection["Mode"] ?? "HS256";
        var isHs256 = string.Equals(mode, "HS256", StringComparison.OrdinalIgnoreCase);

        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

        var defaultScheme = isHs256 ? AuthSchemes.HS256 : AuthSchemes.RS256;

        var authBuilder = builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = defaultScheme;
                options.DefaultChallengeScheme = defaultScheme;
            });

        if (isHs256)
        {
            var hsKeyStr = jwtSection["HsKey"] ?? "";
            if (hsKeyStr.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:HsKey must be at least 32 characters (required in HS256 mode).");

            var hsKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(hsKeyStr));

            authBuilder.AddJwtBearer(AuthSchemes.HS256, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = hsKey,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents();
                options.Events.UseMultiSourceToken();
            });
        }
        else
        {
            var publicPath = Path.Combine(AppContext.BaseDirectory,
                jwtSection["PublicKeyPath"] ?? "keys/public.pem");
            var rsaPublicKey = RsaKeyLoader.LoadPublicKey(publicPath);

            authBuilder.AddJwtBearer(AuthSchemes.RS256, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = rsaPublicKey,
                    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents();
                options.Events.UseMultiSourceToken();
            });
        }

        builder.Services.AddAppAuthorization(builder.Configuration);

        #endregion

        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            }
        });

        var app = builder.Build();

        app.InitializeDataBases()
           .UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(opt => { });
            app.UseCors($"{CorsPoliticType.AllowAll}");
        }
        else
        {
            app.UseCors($"{CorsPoliticType.Production}");
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }
}