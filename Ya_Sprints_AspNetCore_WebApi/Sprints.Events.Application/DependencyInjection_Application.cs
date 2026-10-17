 
using Microsoft.Extensions.DependencyInjection; 
using Sprints.Events.Application.Mapping;
using Sprints.Events.Application.UseCases.DataServices;
using Sprints.Events.Application.UseCases.DataServices.Contracts; 


namespace Sprints.Events.Application.DI;


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

        // AutoMapper (профили в этой же сборке)
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingDtoProfile>();
            cfg.AddProfile<MappingEntityProfile>();
        });

        return services;
    }
}
