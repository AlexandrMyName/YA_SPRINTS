using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Sprints_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations
{
    public static class SwaggerGen_Ext
    {
        public static IServiceCollection AddSwaggerGenWithDocumentation(this IServiceCollection services)
        {

            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Введите токен JWT (без слова Bearer)"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });


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