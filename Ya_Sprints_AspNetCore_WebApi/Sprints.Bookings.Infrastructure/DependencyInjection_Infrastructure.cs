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

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<IInterceptLockings, InterceptLockings>();

        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName));

        // KafkaPublisher — Singleton, IDisposable
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();

        // services.AddHostedService<BookingBackgroundService>();  // [Depricated]
        services.AddHostedService<OutboxProcessor>();              // Outbox → Kafka
        services.AddHostedService<BookingAcceptedSubscriber>();    // Kafka → Pending→Confirmed
        services.AddHostedService<BookingRejectedSubscriber>();    // Kafka → Pending→Rejected

        return services;
    }
}