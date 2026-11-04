using Confluent.Kafka.Extensions.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


namespace Sprints.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddSprintsObservability(
        this IServiceCollection services, IConfiguration configuration,
        string serviceName)
    {

        var otlpEndpoint = configuration["Otlp:Endpoint"] ?? "http://otel-collector:4317";



        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddConfluentKafkaInstrumentation()
                .AddSource("Sprints.Kafka.Consumer")
                // Трейсы отправляем в Collector (или напрямую в Jaeger)
                .AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri(otlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation() 
                .AddPrometheusExporter());

        return services;
    }
}