

namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;


public static class CORS_Ext
{
    
    public static IServiceCollection AddCORS(this IServiceCollection services) 
       => services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => // Первым аргументом указывается название политики
            {
                policy.AllowAnyOrigin()      // Разрешить запросы от любых доменов
                      .AllowAnyMethod()      // Разрешить любые HTTP-методы (GET, POST, PUT и т. д.)
                      .AllowAnyHeader();     // Разрешить любые заголовки в запросе 
            });

            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins("https://myapp.com") // Только этот домен
                      .WithMethods("GET", "POST") // Только эти методы
                      .WithHeaders("Content-Type", "Authorization"); // Только эти заголовки
            });
        });
     
}
