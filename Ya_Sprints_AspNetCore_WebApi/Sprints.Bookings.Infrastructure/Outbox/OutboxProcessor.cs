using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using Sprints.Bookings.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Sprints.Bookings.Infrastructure.Outbox;


public class OutboxProcessor : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;


    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IKafkaPublisher publisher,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken ct)
    {

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
                await Task.Delay(PollInterval, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor: ошибка в цикле");
                try { await Task.Delay(PollInterval, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }


    private async Task ProcessBatchAsync(CancellationToken ct)
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
                await _publisher.PublishRawAsync(msg.Topic, msg.Key, msg.Payload, ct);
                msg.MarkProcessed();
            }
            catch (Exception ex)
            {
                msg.MarkFailed(ex.Message);
                _logger.LogWarning(
                    "Outbox: ошибка публикации {MessageId} (attempt {Attempt}): {Error}",
                    msg.Id, msg.Attempts, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}