using SprintsASP_NetCore_API.Infrastructure.DataAccess.Interceptors;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using SprintsASP_NetCore_API.Application.Abstractions.Security;
using SprintASP_NetCore_API.Infrastructure.BackgroundServices;
using SprintASP_NetCore_API.Infrastructure.Repositories;
using SprintsASP_NetCore_API.Infrastructure.Concurrency;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintsASP_NetCore_API.Infrastructure.Security;
using SprintASP_NetCore_API.Services.Referencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
 

namespace SprintsASP_NetCore_API.Infrastructure.DI;


public static class DependencyInjection_Infrastructure
{

    /// <summary>
    /// Регистрирует всё, что работает с внешним миром:
    /// БД, репозитории, интерцепторы, фоновые воркеры, системные сервисы.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // Персистентность  
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<AuditInterceptor>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();           
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();          

        // Concurrency (in-memory locks)  
        services.AddSingleton<IInterceptLockings, InterceptLockings>();

        // Системные сервисы  
        services.AddSingleton<IReferenciesData, RefDataService>();

        //  Background workers  
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }



  
}