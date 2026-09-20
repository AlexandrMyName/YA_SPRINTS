using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services.Background;
using SprintASP_NetCore_API.Services.Referencies;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintsASP_NetCore_API.Infrastructure.Concurrency;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.Interceptors; 


namespace SprintsASP_NetCore_API.Infrastructure;


public static class DependencyInjection
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
            o.UseNpgsql(config.GetConnectionString("Default")));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<AuditInterceptor>();

        // Concurrency (in-memory locks)  
        services.AddSingleton<IInterceptLockings, InterceptLockings>();

        // Системные сервисы  
        services.AddSingleton<IReferenciesData, RefDataService>();

        //  Background workers  
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}