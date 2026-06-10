using Microsoft.OpenApi.Models;
using Sprint1_Project_ASP_NetCore_API.Filters;
using System.Reflection;

namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations
{
    public static class SwaggerGen_Ext
    {
        public static IServiceCollection AddSwaggerGenWithDocumentation(this IServiceCollection services)
        => services.AddSwaggerGen(options =>
        {
            // Путь к XML-файлу с документацией
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API V1", Version = "v1" });
            options.SwaggerDoc("v2", new OpenApiInfo { Title = "My API V2", Version = "v2" });

        });
    }
}
