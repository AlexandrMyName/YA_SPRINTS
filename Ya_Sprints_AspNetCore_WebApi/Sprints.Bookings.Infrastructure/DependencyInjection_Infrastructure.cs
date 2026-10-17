using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sprints.Bookings.Application.Abstractions;
using Sprints.Bookings.Application.Abstractions;
using Sprints.Bookings.Application.Abstractions.Security;
using Sprints.Bookings.Infrastructure.BackgroundServices;
using Sprints.Bookings.Infrastructure.Concurrency;
using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using Sprints.Bookings.Infrastructure.DataAccess.Interceptors;
using Sprints.Bookings.Infrastructure.Kafka;
using Sprints.Bookings.Infrastructure.Kafka.Subscribtions;
using Sprints.Bookings.Infrastructure.Repositories;
using Sprints.Bookings.Infrastructure.Security; 


namespace Sprints.Bookings.Infrastructure.DI;


public static class DependencyInjection_Infrastructure
{

    /// <summary>
    /// Регистрирует всё, что работает с внешним миром:
    /// БД, репозитории, интерцепторы, фоновые воркеры, системные сервисы.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
      this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<AuditInterceptor>();
        services.AddSingleton<IInterceptLockings, InterceptLockings>(); 

        // Kafka
        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName));
        services.AddHostedService<BookingRejectedSubscriber>();
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();   

        services.AddHostedService<BookingBackgroundService>();

        return services;
    }

    // Refit и Polly 
    // Паттерн SAGA и Паттерн Circuit Breaker (реализуется с помощью Polly) 


}

 