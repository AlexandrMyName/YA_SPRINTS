using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Application.Abstractions.Options;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Infrastructure.Caching;
using Sprints.Events.Infrastructure.Concurrency;
using Sprints.Events.Infrastructure.DataAccess.DbContexts;
using Sprints.Events.Infrastructure.Kafka;
using Sprints.Events.Infrastructure.Outbox;
using Sprints.Events.Infrastructure.Repositories;


namespace Sprints.Events.Infrastructure.DI;

public static class DependencyInjection_Infrastructure
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // EF Core / Postgres  
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        // Repositories / Concurrency  
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddSingleton<IInterceptLockings, InterceptLockings>();

        // Outbox / Kafka  
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IKafkaPublisher, KafkaPublisher>();

        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingRequestedSubscriber>();
        services.AddHostedService<OutboxProcessor>();

        // Redis / Cache  
        services.AddEventsCaching(config);

        return services;
    }
     
    /// <summary>
    /// Кеширование
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static IServiceCollection AddEventsCaching(
        this IServiceCollection services, IConfiguration config)
    {

        services.Configure<CacheOptions>(config.GetSection("Cache"));

        var redisSection = config.GetSection("Redis");
        var enabled = redisSection.GetValue<bool>("Enabled");
        var connectionString = redisSection.GetValue<string>("ConnectionString");

        if (enabled && !string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddStackExchangeRedisCache(o =>
            {
                // abortConnect=false — критично: контейнер стартует даже если Redis ещё не поднялся.
                // connectTimeout/syncTimeout — быстрая деградация вместо зависания запросов.
                o.Configuration = connectionString
                    + ",abortConnect=false,connectTimeout=2000,syncTimeout=2000";
                o.InstanceName = redisSection.GetValue<string>("InstanceName") ?? "sprints:";
            });
             
            services.AddSingleton<RedisEventCache>();
            services.AddSingleton<IEventCache>(sp =>
                new ResilientEventCache(
                    sp.GetRequiredService<RedisEventCache>(),
                    sp.GetRequiredService<ILogger<ResilientEventCache>>()));
        }
        else
        {
         
            services.AddSingleton<IEventCache, NullEventCache>();
        }

        return services;
    }
}