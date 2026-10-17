using Sprints.Bookings.Application.UseCases.DataServices.Contracts;
using Sprints.Bookings.Application.UseCases.DataServices;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Bookings.Application.Mapping;


namespace Sprints.Bookings.Application.DI;


public static class DependencyInjection_Application
{
    /// <summary>
    /// Регистрирует use case'ы и их зависимости из слоя Application.
    /// Никаких DbContext, HttpClient, HostedServices — только Application-сервисы.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases 
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
