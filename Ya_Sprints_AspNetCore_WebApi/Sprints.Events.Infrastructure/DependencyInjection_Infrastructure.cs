using Sprints.Events.Infrastructure.DataAccess.DbContexts; 
using Sprints.Events.Infrastructure.Repositories;
using Sprints.Events.Infrastructure.Concurrency; 
using Microsoft.Extensions.DependencyInjection;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;


namespace Sprints.Events.Infrastructure.DI;


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
        services.AddSingleton<IInterceptLockings, InterceptLockings>(); 

        // Kafka
        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName)); 
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedSubscriber>();

        return services;
    }

} 