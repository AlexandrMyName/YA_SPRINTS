

namespace Sprints.Contracts;


public sealed record BookingRejected
{
    /// <summary>
    /// Уникальный ID сообщения.  
    /// </summary>
    public Guid MessageId { get; init; }
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public string Reason { get; init; } = "";
    public DateTime RejectedAt { get; init; }
}
