using Microsoft.AspNetCore.Mvc;
using Sprint1_Project_ASP_NetCore_API.Filters;


namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;


public static class ConfigureControllersWithCacheProfiles_Ext
{
    /// <summary>
    /// Добавляет поддержку контроллеров с настройками кеширования 
    /// и автоматической валидацией входных и выходных данных
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddControllersWithCacheAndValidation(this IServiceCollection services)
    { 

        services.AddControllers(options =>
        {
           
            options.Filters.Add<ValidateInputModelAttribute>(); // Кастомный Middleware валидации входящих и исходящих данных

            // Определяем профили кеширования
            options.CacheProfiles.Add("Default", new CacheProfile
            {
                Duration = 60,
                Location = ResponseCacheLocation.Any
            });

            options.CacheProfiles.Add("Never", new CacheProfile
            {
                NoStore = true,
                Location = ResponseCacheLocation.None
            });

            options.CacheProfiles.Add("LongTerm", new CacheProfile
            {
                Duration = 3600, // 1 час
                Location = ResponseCacheLocation.Any,
                VaryByHeader = "Accept-Language"
            });
        }).ConfigureApiBehaviorOptions(options =>
        { 
            // Эта опция отключает автоматическую проверку валидации 
            options.SuppressModelStateInvalidFilter = true;

            options.InvalidModelStateResponseFactory = context =>
            {
                // Получаем ошибки валидации          
                var errors = context.ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value!.Errors.Select(e => e.ErrorMessage));

                // Можно задать кастомный формат возвращаемого сообщения
                var customResponse = new
                {
                    Message = "Ошибки валидации",
                    Errors = errors
                };

                // Можно получить экземпляр класса Logger и логировать ошибки валидации
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                var errorsString = string.Join(",", errors.Select(kv => $"{kv.Key}: {kv.Value}"));

                logger.LogError($"Ошибка валидации: {errorsString}");

                return new BadRequestObjectResult(customResponse);
            };
        });

        return services;
    }  
}
