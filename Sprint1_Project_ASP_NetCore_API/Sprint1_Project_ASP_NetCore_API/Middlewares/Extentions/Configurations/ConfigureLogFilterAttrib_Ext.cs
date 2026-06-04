using Sprint1_Project_ASP_NetCore_API.Filters; 


namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;


public static class ConfigureLogFilterAttrib_Ext
{

    /// <summary>
    /// Добавляет фильтр 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddLogFilterAttrib(this IServiceCollection services)
        => services.AddScoped<LogFilterAttribute>();

}
