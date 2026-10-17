using Sprints.Events.Application.Abstractions;
using Sprints.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using Sprints.Contracts;


namespace Sprints.Events.Application.UseCases;


public class BookingConfirmedHandler : IBookingConfirmedHandler
{

    private readonly IRepository<Event> _events;
    private readonly IRepository<InboxMessage> _inbox;
    private readonly IOutboxWriter _outbox;
    private readonly IInterceptLockings _locks;
    private readonly ILogger<BookingConfirmedHandler> _logger;


    public BookingConfirmedHandler(
        IRepository<Event> events,
        IRepository<InboxMessage> inbox,
        IOutboxWriter outbox,
        IInterceptLockings locks,
        ILogger<BookingConfirmedHandler> logger)
    {
        _events = events;
        _inbox = inbox;
        _outbox = outbox;
        _locks = locks;
        _logger = logger;
    }


    public async Task HandleAsync(BookingConfirmed message, CancellationToken ct = default)
    {

        var semaphore = _locks.GetOrAddByEventId(message.EventId);
        await semaphore.WaitAsync(ct);

        try
        {
            // Inbox dedup — до транзакции? Или внутри?
            // Внутри — правильно, чтобы повтор не проскочил между check и save.
            await using var tx = await _events.BeginTransactionAsync(ct);
            try
            {
                // 1. Проверка дубля
                var existing = await _inbox.GetByIdAsync(message.MessageId, ct);
                if (existing.IsSuccesfuly)
                {
                    _logger.LogDebug(
                        "Events: сообщение {MessageId} уже обработано — skip",
                        message.MessageId);
                    await tx.CommitAsync(ct);
                    return;
                }

                // 2. Загружаем событие
                var evResult = await _events.GetByIdAsync(message.EventId, ct);
                if (!evResult.IsSuccesfuly || evResult.Data == null)
                {
                    _logger.LogWarning(
                        "Events: событие {EventId} не найдено для Booking {BookingId}",
                        message.EventId, message.BookingId);

                    await EnqueueRejectedAsync(message, "Событие не найдено", ct);
                    await MarkProcessedAsync(message.MessageId, ct);
                    await _events.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                var ev = evResult.Data;

                // 3. Проверка мест
                if (ev.AvailableSeats < message.SeatsCount)
                {
                    _logger.LogWarning(
                        "Events: нет мест для {EventId} (Available={Avail}, Requested={Req}, Booking {BookingId})",
                        message.EventId, ev.AvailableSeats, message.SeatsCount, message.BookingId);

                    await EnqueueRejectedAsync(message, "Нет свободных мест", ct);
                    await MarkProcessedAsync(message.MessageId, ct);
                    await _events.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                // 4. Успех — уменьшаем
                ev.AvailableSeats -= message.SeatsCount;
                await _events.UpdateAsync(ev, ct);
                await MarkProcessedAsync(message.MessageId, ct);
                await _events.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "Events: уменьшены места {EventId} на {Count}, осталось {Left}",
                    message.EventId, message.SeatsCount, ev.AvailableSeats);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }


    private Task EnqueueRejectedAsync(BookingConfirmed original, string reason, CancellationToken ct)
        => _outbox.EnqueueAsync(
            topic: KafkaTopics.BookingRejected,
            key: original.EventId.ToString(),
            payload: new BookingRejected
            {
                BookingId = original.BookingId,
                EventId = original.EventId,
                Reason = reason,
                RejectedAt = DateTime.UtcNow
            },
            ct: ct);


    private async Task MarkProcessedAsync(Guid messageId, CancellationToken ct)
    {
        await _inbox.AddAsync(InboxMessage.Create(messageId, nameof(BookingConfirmed)), ct);
    }
}