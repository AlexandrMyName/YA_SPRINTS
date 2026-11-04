using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Sprints.Observability;
using Sprints.Users.Application.Abstractions.Options;
using Sprints.Users.Application.DI;
using Sprints.Users.Infrastructure.DI;
using Sprints.Users.Infrastructure.Security;
using Sprints.Users.Middlewares;
using Sprints.Users.Middlewares.Extentions.Configurations;
using Sprints.Users.Presentation.Extensions;
using Sprints.Users.Presentation.Extentions;
using System.Text;

[assembly: ApiController]


namespace Sprints.Users.Api;

public class Program
{

    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Presentation-конфигурация
        builder.Services
            .AddCorsPolicies()
            .AddControllersWithCacheAndValidation()
            .AddEndpointsApiExplorer()
            .AddSwaggerGenWithDocumentation()
            .AddApiVersioningCustom();

        // Слои
        builder.Services
            .AddApplication()
            .AddInfrastructure(builder.Configuration)
            .AddSprintsObservability(builder.Configuration, "users-service");


        #region SECURITY

        builder.Services.Configure<JwtOptions>(
            builder.Configuration.GetSection(JwtOptions.SectionName));

        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

        var mode = jwtSection["Mode"] ?? "HS256";
        var isHs256 = string.Equals(mode, "HS256", StringComparison.OrdinalIgnoreCase);

        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

        // Схема по умолчанию — по Mode  
        var defaultScheme = isHs256 ? AuthSchemes.HS256 : AuthSchemes.RS256;

        var authBuilder = builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = defaultScheme;
                options.DefaultChallengeScheme = defaultScheme;
            });

        if (isHs256)
        {
            // HS256-режим: только HS256-схема 
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
            // RS256-режим: только RS256-схема  
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
         
        // логи в JSON
        builder.Host.UseSerilog((ctx, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            if (context.HostingEnvironment.IsDevelopment())
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            }
        });

        var app = builder.Build();
        app.UseSerilogRequestLogging();


        app.UseOpenTelemetryPrometheusScrapingEndpoint();
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

        // app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers(); 

        app.Run();
    }
}