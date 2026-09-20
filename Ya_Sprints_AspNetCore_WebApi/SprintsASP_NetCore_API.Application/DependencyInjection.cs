using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Services; 
using Sprints_Project_ASP_NetCore_API.Services.DataServices;
using Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;
using SprintASP_NetCore_API.Services.DataServices;


namespace SprintsASP_NetCore_API.Application;


public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует use case'ы и их зависимости из слоя Application.
    /// Никаких DbContext, HttpClient, HostedServices — только Application-сервисы.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ---- Use cases ----
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
