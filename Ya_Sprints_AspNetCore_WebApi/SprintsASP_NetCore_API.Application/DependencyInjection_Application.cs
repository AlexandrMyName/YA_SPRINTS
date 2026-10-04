 
using Microsoft.Extensions.DependencyInjection; 
using SprintASP_NetCore_API.Application.UseCases.DataServices;
using SprintASP_NetCore_API.Application.Mapping;
using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;


namespace SprintsASP_NetCore_API.Application.DI;


public static class DependencyInjection_Application
{
    /// <summary>
    /// Регистрирует use case'ы и их зависимости из слоя Application.
    /// Никаких DbContext, HttpClient, HostedServices — только Application-сервисы.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases
        services.AddScoped<IEventService, EventsService>();
        services.AddScoped<IBookingService, BookingService>();

        // AutoMapper (профили в этой же сборке)
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingDtoProfile>();
            cfg.AddProfile<MappingEntityProfile>();
        });

        return services;
    }
}
