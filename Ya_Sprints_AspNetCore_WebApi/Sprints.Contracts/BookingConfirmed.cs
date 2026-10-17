 

namespace Sprints.Contracts;


/// <summary>
/// Событие: бронь подтверждена.
/// Публикует Bookings, потребляет Events.
/// </summary>
public sealed record BookingConfirmed
{
    /// <summary>
    /// Уникальный ID сообщения. Генерируется при создании Outbox-записи
    /// </summary>
    public Guid MessageId { get; init; }
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int SeatsCount { get; init; }
    public DateTime ConfirmedAt { get; init; }
}
