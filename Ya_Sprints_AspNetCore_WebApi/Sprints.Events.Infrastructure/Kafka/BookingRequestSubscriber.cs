using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;
using Sprints.Events.Application.Abstractions;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Sprints.Events.Infrastructure.Kafka;

public class BookingRequestedSubscriber : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<BookingRequestedSubscriber> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ActivitySource ActivitySource = new("Sprints.Kafka.Consumer");

    public BookingRequestedSubscriber(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingRequestedSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(async () =>
        {
            try
            {
                await ConsumeLoopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"==================================================");
                Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА В КОНСЬЮМЕРЕ EVENTS: {ex.Message}");
                Console.WriteLine($"==================================================");
                throw;
            }
        }, stoppingToken);

    private async Task ConsumeLoopAsync(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingRequested);
        _logger.LogInformation("Kafka: Events подписан на {Topic}", KafkaTopics.BookingRequested);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(ct);
                    if (result?.Message?.Value == null) continue;

                    // Безопасное извлечение traceparent
                    ActivityContext parentContext = default;
                    byte[]? traceparentHeader = null;
                    if (result.Message.Headers != null && result.Message.Headers.TryGetLastBytes("traceparent", out var headerBytes))
                    {
                        traceparentHeader = headerBytes;
                    }

                    if (traceparentHeader != null)
                    {
                        var traceparent = Encoding.UTF8.GetString(traceparentHeader);
                        ActivityContext.TryParse(traceparent, null, out parentContext);
                    }

                    using var activity = ActivitySource.StartActivity(
                        $"Kafka Consume {result.Topic}",
                        ActivityKind.Consumer,
                        parentContext);

                    var message = JsonSerializer.Deserialize<BookingRequested>(result.Message.Value, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Kafka: пустое BookingRequested, skip");
                        consumer.Commit(result);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IBookingRequestedHandler>();

                    await handler.HandleAsync(message, ct);
                    consumer.Commit(result);

                    _logger.LogInformation("Kafka: Успешно обработано BookingRequested {MessageId}", message.MessageId);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка обработки BookingRequested (Partition={P}, Offset={O})",
                        result?.Partition.Value, result?.Offset.Value);
                    try { await Task.Delay(1000, ct); } catch { break; }
                }
            }
        }
        finally
        {
            try { consumer.Close(); } catch (Exception ex) { _logger.LogWarning(ex, "Kafka: ошибка Close"); }
        }
    }
}