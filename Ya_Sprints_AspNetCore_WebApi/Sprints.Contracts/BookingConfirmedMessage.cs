

namespace Sprints.Contracts;


public sealed record BookingConfirmedMessage(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime ConfirmedAtUtc);
