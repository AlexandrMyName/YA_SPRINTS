using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Sprint1_Project_ASP_NetCore_API.Filters;


namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;


public static class ConfigureApiVersioned_Ext
{

    public static IServiceCollection AddApiVersioningCustom(this IServiceCollection services)
    {

        services.AddApiVersioning(options =>
          {
              options.ReportApiVersions = true;
              options.AssumeDefaultVersionWhenUnspecified = true;
              options.DefaultApiVersion = new ApiVersion(1, 0);
              options.ApiVersionReader = new UrlSegmentApiVersionReader(); // важно для {version:apiVersion}
          });            // Добавляет верссионирование проекта

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }  
}
