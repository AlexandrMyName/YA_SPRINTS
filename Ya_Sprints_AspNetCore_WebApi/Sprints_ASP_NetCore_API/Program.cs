using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SprintASP_NetCore_API.Presentation.Extentions;
using Sprints_Project_ASP_NetCore_API.Middlewares;
using Sprints_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;   
using Sprints_Project_ASP_NetCore_API.Presentation.Extensions;
using SprintsASP_NetCore_API.Application.Abstractions.Options;
using SprintsASP_NetCore_API.Application.DI;
using SprintsASP_NetCore_API.Infrastructure.DI;
using SprintsASP_NetCore_API.Infrastructure.Security;
using System.Text;


[assembly: ApiController] // Все контроллеры будут API


namespace Sprints_Project_ASP_NetCore_API;


public class Program
{

    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Presentation-конфигурация  
        builder.Services
            .AddCorsPolicies()                  // Presentation
            .AddControllersWithCacheAndValidation() // Presentation (MVC + ActionFilters)
            .AddEndpointsApiExplorer()
            .AddSwaggerGenWithDocumentation()
            .AddApiVersioningCustom();          // Presentation

        // Слои  
        builder.Services 
            .AddApplication()                          // Application
            .AddInfrastructure(builder.Configuration); // Infrastructure

        #region SECURITY

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

        var publicPath = Path.Combine(AppContext.BaseDirectory,
            jwtSection["PublicKeyPath"] ?? "keys/public.pem");
        var rsaPublicKey = RsaKeyLoader.LoadPublicKey(publicPath);

        var hsKeyStr = jwtSection["HsKey"] ?? "";
        if (hsKeyStr.Length < 32)
            throw new InvalidOperationException("Jwt:HsKey must be at least 32 characters.");
        var hsKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(hsKeyStr));

        var issuer   = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthSchemes.RS256;
                options.DefaultChallengeScheme    = AuthSchemes.RS256;
            })
            .AddJwtBearer(AuthSchemes.RS256, options =>
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
            })
            .AddJwtBearer(AuthSchemes.HS256, options =>
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

        builder.Services.AddAppAuthorization();
         
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

        // Pipeline  
        app.InitializeDataBases()                       // из Presentation.Extensions (бывший Data.DataAccess)
           .UseMiddleware<GlobalExceptionMiddleware>(); // Presentation

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