using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;
using Sprints.Events.Application.Abstractions;
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
        consumer.Subscribe(KafkaTopics.BookingRequested);
        _logger.LogInformation(
            "Kafka: Events подписан на {Topic}, group={Group}",
            KafkaTopics.BookingRequested, _options.ConsumerGroup);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(ct);
                    if (result?.Message?.Value == null) continue;

                    var message = JsonSerializer.Deserialize<BookingRequested>(
                        result.Message.Value, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Kafka: пустое BookingRequested, skip");
                        consumer.Commit(result);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider
                        .GetRequiredService<IBookingRequestedHandler>();

                    await handler.HandleAsync(message, ct);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка Consume, skip offset");
                    if (result != null) consumer.Commit(result);
                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }
                catch (Exception ex)
                {
                    // НЕ коммитим — Kafka доставит повторно, Inbox защитит
                    _logger.LogError(ex,
                        "Kafka: ошибка обработки BookingRequested (offset НЕ коммичен). " +
                        "Partition={P}, Offset={O}",
                        result?.Partition.Value, result?.Offset.Value);
                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        finally
        {
            try { consumer.Close(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Kafka: ошибка Close consumer"); }
        }
    }
}