  

namespace Sprints.Contracts;


/// <summary>
/// Bookings → Events: «Хочу зарезервировать место».
/// </summary>
public sealed record BookingRequested
{
    public Guid MessageId { get; init; }
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int SeatsCount { get; init; }
    public DateTime RequestedAt { get; init; }
}