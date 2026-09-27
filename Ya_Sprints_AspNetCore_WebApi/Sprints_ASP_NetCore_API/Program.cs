using Sprints_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;   
using Sprints_Project_ASP_NetCore_API.Presentation.Extensions;
using Sprints_Project_ASP_NetCore_API.Middlewares;
using SprintsASP_NetCore_API.Infrastructure.DI;
using SprintsASP_NetCore_API.Application.DI;
using Microsoft.AspNetCore.Mvc;


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
           
            .AddApplication()                                 // Application
            .AddInfrastructure(builder.Configuration); // Infrastructure

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
        app.MapControllers();
        app.Run();
    }
}