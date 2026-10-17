using Sprints.Events.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;
using System.Text.Json;
using Confluent.Kafka;


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
        // Consume() — блокирующий. Task.Run, чтобы не блокировать старт приложения.
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken ct)
    {

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false            // коммитим вручную после обработки
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
                    if (result?.Message?.Value == null) continue;

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IBookingConfirmedHandler>();

                    var message = JsonSerializer.Deserialize<BookingConfirmed>(
                        result.Message.Value, JsonOptions);

                    if (message == null)
                    {
                        _logger.LogWarning("Kafka: пустое сообщение, skip");
                        consumer.Commit(result);
                        continue;
                    }

                    // HandleAsync — синхронно, потому что Consume() блокирующий
                    handler.HandleAsync(message, ct).GetAwaiter().GetResult();

                    // commit только после успеха
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка чтения сообщения");
                    // не коммитим — попробуем снова
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка обработки сообщения. Skip offset.");
                    // Чтобы не застрять на «ядовитом» сообщении — коммитим и идём дальше
                    // Кинуть в DLT
                    if (result != null) consumer.Commit(result);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka: consumer закрыт");
        }
    }
}