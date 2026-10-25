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

    public BookingAcceptedSubscriber(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingAcceptedSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
        => Task.Run(() => ConsumeLoopAsync(ct), ct);

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

                    var message = JsonSerializer.Deserialize<BookingAccepted>(
                        result.Message.Value, JsonOptions);
                    if (message == null) { consumer.Commit(result); continue; }

                    await HandleAsync(message, ct);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка Consume BookingAccepted");
                    if (result != null) consumer.Commit(result);
                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Kafka: ошибка BookingAccepted (offset НЕ коммичен). Partition={P}, Offset={O}",
                        result?.Partition.Value, result?.Offset.Value);
                    try { await Task.Delay(1000, ct); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        finally
        {
            try { consumer.Close(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Kafka: ошибка Close"); }
        }
    }

    private async Task HandleAsync(BookingAccepted message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Inbox первым
            try
            {
                db.InboxMessages.Add(
                    InboxMessage.Create(message.MessageId, nameof(BookingAccepted)));
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (IsDuplicateKey(ex))
            {
                _logger.LogDebug(
                    "Inbox: BookingAccepted {MessageId} уже обработан",
                    message.MessageId);
                await tx.CommitAsync(ct);
                return;
            }

            var booking = await db.Bookings.FindAsync(new object[] { message.BookingId }, ct);
            if (booking == null)
            {
                _logger.LogWarning(
                    "Bookings: бронь {BookingId} не найдена при Accepted",
                    message.BookingId);
                await tx.CommitAsync(ct);
                return;
            }

            if (booking.Status != BookingStatus.Pending)
            {
                _logger.LogDebug(
                    "Bookings: бронь {BookingId} уже {Status}, Accepted проигнорирован",
                    message.BookingId, booking.Status);
                await tx.CommitAsync(ct);
                return;
            }

            booking.Confirm();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Bookings: бронь {BookingId} → Confirmed", message.BookingId);
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
            if (cur is PostgresException pg &&
                pg.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
        return false;
    }
}