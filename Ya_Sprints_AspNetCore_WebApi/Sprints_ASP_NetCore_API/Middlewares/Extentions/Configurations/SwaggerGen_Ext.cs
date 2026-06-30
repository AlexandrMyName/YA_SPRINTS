using Microsoft.OpenApi.Models;
using Sprint1_Project_ASP_NetCore_API.Filters;
using System.Reflection;

namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations
{
    public static class SwaggerGen_Ext
    {
        public static IServiceCollection AddSwaggerGenWithDocumentation(this IServiceCollection services)
        {
             
            services.AddSwaggerGen(options =>
            { 
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath); 
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "YA Sprint One", Version = "v1" });
                options.OrderActionsBy(apiDesc =>
                {
                    var httpMethod = apiDesc.HttpMethod?.ToUpperInvariant();
                    return httpMethod switch
                    {
                        "GET" => "A_GET",
                        "POST" => "B_POST",
                        "PUT" => "C_PUT",
                        "DELETE" => "D_DELETE",
                        _ => $"Z_{httpMethod}"
                    };
                });
                 
            });
            return services;
        }  
    }
} 