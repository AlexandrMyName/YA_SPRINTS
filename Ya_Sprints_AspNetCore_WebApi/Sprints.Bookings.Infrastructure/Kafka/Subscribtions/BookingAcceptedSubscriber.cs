using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Sprints.Bookings.Domain.Entities;
using Sprints.Bookings.Domain.ValueObjects;
using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using Sprints.Contracts;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Sprints.Bookings.Infrastructure.Kafka;

public class BookingAcceptedSubscriber : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<BookingAcceptedSubscriber> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ActivitySource ActivitySource = new("Sprints.Kafka.Consumer");

    public BookingAcceptedSubscriber(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingAcceptedSubscriber> logger)
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
                _logger.LogError(ex, $"КРИТИЧЕСКАЯ ОШИБКА В КОНСЬЮМЕРЕ: {ex.Message}");
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
        consumer.Subscribe(KafkaTopics.BookingAccepted);
        _logger.LogInformation("Kafka: Bookings подписан на {Topic}", KafkaTopics.BookingAccepted);

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

                    var message = JsonSerializer.Deserialize<BookingAccepted>(result.Message.Value, JsonOptions);
                    if (message == null) { consumer.Commit(result); continue; }

                    await HandleAsync(message, ct);
                    consumer.Commit(result);

                    _logger.LogInformation("Kafka: Успешно обработано BookingAccepted {MessageId}", message.MessageId);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка BookingAccepted (Partition={P}, Offset={O})",
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

    private async Task HandleAsync(BookingAccepted message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            try
            {
                db.InboxMessages.Add(InboxMessage.Create(message.MessageId, nameof(BookingAccepted)));
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (IsDuplicateKey(ex))
            {
                _logger.LogDebug("Inbox: BookingAccepted {MessageId} уже обработан", message.MessageId);
                await tx.CommitAsync(ct);
                return;
            }

            var booking = await db.Bookings.FindAsync(new object[] { message.BookingId }, ct);
            if (booking == null || booking.Status != BookingStatus.Pending)
            {
                await tx.CommitAsync(ct);
                return;
            }

            booking.Confirm();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Bookings: бронь {BookingId} → Confirmed", message.BookingId);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static bool IsDuplicateKey(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
            if (cur is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
        return false;
    }
}