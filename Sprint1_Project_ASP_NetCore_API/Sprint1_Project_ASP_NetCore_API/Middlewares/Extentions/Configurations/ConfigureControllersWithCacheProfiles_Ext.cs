using Microsoft.AspNetCore.Mvc;


namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;


public static class ConfigureControllersWithCacheProfiles_Ext
{

    public static IServiceCollection AddControllersWithCacheProfiles(this IServiceCollection services)
    { 

        services.AddControllers(options =>
        {
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
        });

        return services;
    }  
}
