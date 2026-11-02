using Confluent.Kafka.Extensions.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;


namespace Sprints.Observability;
 

public static class ObservabilityExtensions
{

    public static IServiceCollection AddSprintsObservability(
        this IServiceCollection services,
        string serviceName)
    {

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddConfluentKafkaInstrumentation()
                    .AddOtlpExporter(opt =>
                    {
                        // Отправляем в OTel Collector
                        opt.Endpoint = new Uri("http://otel-collector:4317");
                    }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation() // Входящие Http
                .AddHttpClientInstrumentation() // Исходящие
                .AddRuntimeInstrumentation()
                 // .AddPrometheusExporter()); // Экспорт метрик для Prometheus
                 .AddOtlpExporter(opt =>
                 {
                     opt.Endpoint = new Uri("http://otel-collector:4317");
                 }));
        return services;
    }
}
