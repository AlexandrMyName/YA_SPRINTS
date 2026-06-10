using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos; 
using Sprint1_Project_ASP_NetCore_API.Services.DataServices;


namespace Sprint1_Project_ASP_NetCore_API.Services.Extentions;


public static class AddServicesExtention
{
    /// <summary>
    /// Добавляет сервисы в контейнер зависимостей
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        services.AddSingleton<IDataStorageService<EventDto>, EventsService>();
        return services;
    }
}
