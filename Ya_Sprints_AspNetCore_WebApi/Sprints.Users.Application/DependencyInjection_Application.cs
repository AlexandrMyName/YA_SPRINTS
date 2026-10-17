using Sprints.Users.Application.UseCases.DataServices.Contracts;
using Sprints.Users.Application.UseCases.DataServices;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Users.Application.Mapping;


namespace Sprints.Users.Application.DI;


public static class DependencyInjection_Application
{

    /// <summary>
    /// Регистрирует use case'ы и их зависимости из слоя Application.
    /// Никаких DbContext, HttpClient, HostedServices — только Application-сервисы.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases 
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();

        // AutoMapper (профили в этой же сборке)
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingDtoProfile>();
            cfg.AddProfile<MappingEntityProfile>();
        });

        return services;
    }
}
