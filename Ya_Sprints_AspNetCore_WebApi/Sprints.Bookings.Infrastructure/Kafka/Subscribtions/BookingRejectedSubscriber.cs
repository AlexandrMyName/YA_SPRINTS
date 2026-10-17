using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Bookings.Domain.ValueObjects;
using Sprints.Bookings.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;
using System.Text.Json;
using Confluent.Kafka;


namespace Sprints.Bookings.Infrastructure.Kafka;


public class BookingRejectedSubscriber : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<BookingRejectedSubscriber> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    public BookingRejectedSubscriber(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingRejectedSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }


    protected override Task ExecuteAsync(CancellationToken ct)
        => Task.Run(() => ConsumeLoop(ct), ct);


    private void ConsumeLoop(CancellationToken ct)
    {

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false        // коммит вручную
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingRejected);
        _logger.LogInformation("Kafka: Bookings подписан на {Topic}", KafkaTopics.BookingRejected);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(ct);
                    if (result?.Message?.Value == null) continue;

                    var message = JsonSerializer.Deserialize<BookingRejected>(
                        result.Message.Value, JsonOptions);
                    if (message == null) { consumer.Commit(result); continue; }

                    HandleAsync(message, ct).GetAwaiter().GetResult();

                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (ConsumeException ex) { _logger.LogError(ex, "Kafka: ошибка Consume"); }
                catch (Exception ex)
                {
                    // Не коммитим — Kafka доставит повторно. Inbox обеспечит идемпотентность.
                    _logger.LogError(ex, "Kafka: ошибка обработки BookingRejected");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleAsync(BookingRejected message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // Inbox: дедупликация
            var alreadyProcessed = await db.InboxMessages.FindAsync(
                new object[] { message.MessageId }, ct);

            if (alreadyProcessed != null)
            {
                _logger.LogDebug(
                    "Inbox: сообщение {MessageId} уже обработано — skip", message.MessageId);
                await tx.CommitAsync(ct);
                return;
            }

            // Загружаем бронь
            var booking = await db.Bookings.FindAsync(new object[] { message.BookingId }, ct);
            if (booking == null)
            {
                _logger.LogWarning(
                    "Bookings: бронь {BookingId} не найдена при BookingRejected", message.BookingId);
                // Всё равно записываем в Inbox — чтобы не переобрабатывать
                db.InboxMessages.Add(InboxMessage.Create(message.MessageId, nameof(BookingRejected)));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }

            // Идемпотентность на уровне бизнес-логики
            if (booking.Status == BookingStatus.Rejected)
            {
                _logger.LogDebug("Bookings: бронь {BookingId} уже Rejected", message.BookingId);
                db.InboxMessages.Add(InboxMessage.Create(message.MessageId, nameof(BookingRejected)));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                _logger.LogWarning(
                    "Bookings: бронь {BookingId} уже Cancelled, Rejected проигнорирован", message.BookingId);
                db.InboxMessages.Add(InboxMessage.Create(message.MessageId, nameof(BookingRejected)));
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }

            // Меняем статус + пишем в Inbox в одной транзакции
            booking.Reject();
            db.InboxMessages.Add(InboxMessage.Create(message.MessageId, nameof(BookingRejected)));

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogWarning(
                "Bookings: бронь {BookingId} → Rejected. Причина: {Reason}",
                message.BookingId, message.Reason);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}