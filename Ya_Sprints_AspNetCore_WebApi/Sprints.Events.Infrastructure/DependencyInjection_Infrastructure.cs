using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Infrastructure.Concurrency; 
using Sprints.Events.Infrastructure.DataAccess.DbContexts; 
using Sprints.Events.Infrastructure.Kafka;
using Sprints.Events.Infrastructure.Outbox;
using Sprints.Events.Infrastructure.Repositories;


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
         
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName));
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedSubscriber>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

} 