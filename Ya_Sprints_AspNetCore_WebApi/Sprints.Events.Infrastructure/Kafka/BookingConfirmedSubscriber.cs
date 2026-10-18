using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;
using Sprints.Events.Application.Abstractions;
using System.Text.Json;


namespace Sprints.Events.Infrastructure.Kafka;


public class BookingConfirmedSubscriber : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<BookingConfirmedSubscriber> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BookingConfirmedSubscriber(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingConfirmedSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoopAsync(stoppingToken), stoppingToken);

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
        consumer.Subscribe(KafkaTopics.BookingConfirmed);
        _logger.LogInformation(
            "Kafka: подписан на {Topic}, group={Group}",
            KafkaTopics.BookingConfirmed, _options.ConsumerGroup);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(ct);

                    // null-защита
                    if (result?.Message?.Value == null) continue;

                    // Десериализация — до создания scope, чтобы scope не висел
                    var message = JsonSerializer.Deserialize<BookingConfirmed>(
                        result.Message.Value, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Kafka: пустое сообщение, skip");
                        consumer.Commit(result);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider
                        .GetRequiredService<IBookingConfirmedHandler>();

                    // async/await без sync-over-async
                    await handler.HandleAsync(message, ct);

                    // commit только после успеха
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // graceful shutdown
                    break;
                }
                catch (ConsumeException ex)
                {
                    // ошибка чтения — коммитим, чтобы не застрять
                    _logger.LogError(ex, "Kafka: ошибка Consume, пропускаем сообщение");
                    if (result != null) consumer.Commit(result);

                    // пауза против спама при недоступном брокере
                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }
                catch (Exception ex)
                {
                    // При падении обработки Kafka переотправит,
                    // Inbox-таблица защитит от повторного применения.
                    _logger.LogError(ex,
                        "Kafka: ошибка обработки сообщения (offset НЕ коммичен, будет retry). " +
                        "Partition={Partition}, Offset={Offset}",
                        result?.Partition.Value, result?.Offset.Value);

                    // Короткая пауза, чтобы не крутиться в горячем цикле
                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        finally
        {
            try
            {
                consumer.Close();
                _logger.LogInformation("Kafka: consumer закрыт");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka: ошибка при закрытии consumer");
            }
        }
    }
}