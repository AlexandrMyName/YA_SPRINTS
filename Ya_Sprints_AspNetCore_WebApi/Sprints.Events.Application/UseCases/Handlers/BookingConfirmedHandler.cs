using Sprints.Events.Application.Abstractions;
using Sprints.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using Sprints.Contracts;


namespace Sprints.Events.Application.UseCases;


public class BookingConfirmedHandler : IBookingConfirmedHandler
{

    private readonly IRepository<Event> _repository;
    private readonly IInterceptLockings _locks;
    private readonly IKafkaPublisher _kafka;
    private readonly ILogger<BookingConfirmedHandler> _logger;


    public BookingConfirmedHandler(
        IRepository<Event> repository,
        IInterceptLockings locks,
        IKafkaPublisher kafka,
        ILogger<BookingConfirmedHandler> logger)
    {
        _repository = repository;
        _locks = locks;
        _kafka = kafka;
        _logger = logger;
    }


    public async Task HandleAsync(BookingConfirmed message, CancellationToken ct = default)
    {
        var semaphore = _locks.GetOrAddByEventId(message.EventId);
        await semaphore.WaitAsync(ct);

        await using var tx = await _repository.BeginTransactionAsync(ct);
        try
        {
            var found = await _repository.GetByIdAsync(message.EventId, ct);

            // Событие не найдено — публикуем Rejected
            if (!found.IsSuccesfuly || found.Data == null)
            {
                _logger.LogWarning(
                    "Events: событие {EventId} не найдено для Booking {BookingId} — публикуем BookingRejected",
                    message.EventId, message.BookingId);

                await PublishRejectedAsync(message, "Событие не найдено", ct);
                await tx.CommitAsync(ct);
                return;
            }

            var ev = found.Data;

            // Нет мест — публикуем Rejected
            if (ev.AvailableSeats < message.SeatsCount)
            {
                _logger.LogWarning(
                    "Events: нет мест для {EventId} (Available={Avail}, Requested={Req}, Booking {BookingId}) — публикуем BookingRejected",
                    ev.AvailableSeats, message.SeatsCount, message.EventId, message.BookingId);

                await PublishRejectedAsync(message, "Нет свободных мест", ct);
                await tx.CommitAsync(ct);
                return;
            }

            // Уменьшаем места
            ev.AvailableSeats -= message.SeatsCount;
            await _repository.UpdateAsync(ev, ct);
            await _repository.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "Events: уменьшены места события {EventId} на {Count}, осталось {Left}",
                message.EventId, message.SeatsCount, ev.AvailableSeats);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }


    private Task PublishRejectedAsync(BookingConfirmed original, string reason, CancellationToken ct)
        => _kafka.PublishAsync(
            topic: KafkaTopics.BookingRejected,
            key: original.EventId.ToString(),
            message: new BookingRejected
            {
                BookingId = original.BookingId,
                EventId = original.EventId,
                Reason = reason,
                RejectedAt = DateTime.UtcNow
            },
            ct);
}