using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Services;
using SprintASP_NetCore_API.Services.Background;
using SprintASP_NetCore_API.Services.DataServices;
using SprintASP_NetCore_API.Services.Referencies;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos; 
using Sprints_Project_ASP_NetCore_API.Services.DataServices;


namespace Sprints_Project_ASP_NetCore_API.Services.Extentions;


public static class AddServicesExtention
{
    /// <summary>
    /// Добавляет сервисы в контейнер зависимостей
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {

        services.AddSingleton<IDataStorageService<EventDto>, EventsService>(); // Сервис событий  
        services.AddSingleton<IBookingService, BookingService>();  // Сервис бронирований 
        services.AddSingleton<IReferenciesData, RefDataService>(); // Дополнительный класс для ссылок данных 

        // Hosted Services
        services.AddHostedService<BookingBackgroundService>(); // Сервис выполнения логики бронирования 
        return services;
    }
}
