using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using Sprints.Bookings.Infrastructure.BackgroundServices;
using Sprints.Bookings.Infrastructure.Repositories;
using Sprints.Bookings.Infrastructure.Concurrency;
using Sprints.Bookings.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Bookings.Infrastructure.Outbox;
using Sprints.Bookings.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;


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
        services.AddScoped<IInterceptLockings, InterceptLockings>();

        // Outbox
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        // Kafka
        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName));

        // Background services
        services.AddHostedService<BookingBackgroundService>();   // Pending → Confirmed + Outbox
        services.AddHostedService<OutboxProcessor>();            // Outbox  → Kafka
        services.AddHostedService<BookingRejectedSubscriber>();  // Kafka   → Inbox + Booking.Rejected

        return services;
    }  
}

 