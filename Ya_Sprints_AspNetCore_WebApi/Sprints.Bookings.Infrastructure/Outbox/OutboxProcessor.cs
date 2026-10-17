using Sprints.Bookings.Infrastructure.DataAccess.DbContexts; 
using Microsoft.Extensions.DependencyInjection;
using Sprints.Bookings.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Confluent.Kafka;


namespace Sprints.Bookings.Infrastructure.Outbox;


public class OutboxProcessor : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Создаём продюсер ОДИН РАЗ — потокобезопасен, тяжёлый
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(producer, ct);
                await Task.Delay(PollInterval, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor: ошибка в цикле");
                await Task.Delay(PollInterval, ct);
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private async Task ProcessBatchAsync(IProducer<string, string> producer, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        foreach (var msg in messages)
        {
            try
            {
                await producer.ProduceAsync(
                    msg.Topic,
                    new Message<string, string> { Key = msg.Key, Value = msg.Payload },
                    ct);

                msg.MarkProcessed();
                _logger.LogInformation(
                    "Outbox: отправлено {MessageId} в {Topic}",
                    msg.Id, msg.Topic);
            }
            catch (Exception ex)
            {
                msg.MarkFailed(ex.Message);
                _logger.LogWarning(
                    "Outbox: ошибка {MessageId} (attempt {Attempt}): {Error}",
                    msg.Id, msg.Attempts, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}