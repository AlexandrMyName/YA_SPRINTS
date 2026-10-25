using Sprints.Events.Application.Abstractions;
using Sprints.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using Sprints.Contracts;


namespace Sprints.Events.Application.UseCases;


public class BookingRequestedHandler : IBookingRequestedHandler
{

    private readonly IRepository<Event> _events;
    private readonly IRepository<InboxMessage> _inbox;
    private readonly IOutboxWriter _outbox;
    private readonly IInterceptLockings _locks;
    private readonly IEventCache _cache;
    private readonly ILogger<BookingRequestedHandler> _logger;


    public BookingRequestedHandler(
        IRepository<Event> events,
        IRepository<InboxMessage> inbox,
        IOutboxWriter outbox,
        IInterceptLockings locks,
        IEventCache cache,
        ILogger<BookingRequestedHandler> logger)
    {
        _events = events;
        _inbox = inbox;
        _outbox = outbox;
        _locks = locks;
        _cache = cache;
        _logger = logger;
    }


    public async Task HandleAsync(BookingRequested message, CancellationToken ct = default)
    {

        var semaphore = _locks.GetOrAddByEventId(message.EventId);
        await semaphore.WaitAsync(ct);

        try
        {
            // Флаг: менялась ли сущность Event. Инвалидация только когда true —
            // в ветках Rejected/duplicate кеш трогать нечего.
            bool eventChanged = false;

            await using (var tx = await _events.BeginTransactionAsync(ct))
            {
                try
                {
                    // Inbox dedup
                    var existing = await _inbox.GetByIdAsync(message.MessageId, ct);
                    if (existing.IsSuccesfuly)
                    {
                        _logger.LogDebug(
                            "Events: BookingRequested {MessageId} уже обработан — skip",
                            message.MessageId);
                        await tx.CommitAsync(ct);
                        return;
                    }

                    var evResult = await _events.GetByIdAsync(message.EventId, ct);

                    // Случай A: событие не найдено → Rejected
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

                    // Случай B: нет мест → Rejected
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

                    // Случай C: успех — уменьшаем места + BookingAccepted
                    ev.AvailableSeats -= message.SeatsCount;
                    await _events.UpdateAsync(ev, ct);

                    await _outbox.EnqueueAsync(
                        topic: KafkaTopics.BookingAccepted,
                        key: message.EventId.ToString(),
                        payload: new BookingAccepted
                        {
                            BookingId = message.BookingId,
                            EventId = message.EventId,
                            AcceptedAt = DateTime.UtcNow
                        },
                        ct: ct);

                    await MarkProcessedAsync(message.MessageId, ct);
                    await _events.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    eventChanged = true;

                    _logger.LogInformation(
                        "Events: зарезервировано {Count} мест для {EventId}, осталось {Left} (Booking {BookingId})",
                        message.SeatsCount, message.EventId, ev.AvailableSeats, message.BookingId);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            }

            // Инвалидация вне транзакции, только при успехе.
            // Top-10 не инвалидируем — бронь частая, TTL 60 c обновит его сам.
            if (eventChanged)
            {
                await _cache.InvalidateEventAsync(message.EventId);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private Task EnqueueRejectedAsync(BookingRequested original, string reason, CancellationToken ct)
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

    private Task MarkProcessedAsync(Guid messageId, CancellationToken ct)
        => _inbox.AddAsync(InboxMessage.Create(messageId, nameof(BookingRequested)), ct);
}