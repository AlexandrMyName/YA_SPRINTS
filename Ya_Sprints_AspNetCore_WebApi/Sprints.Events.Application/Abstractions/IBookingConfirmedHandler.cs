using Sprints.Contracts; 


namespace Sprints.Events.Application.Abstractions;

public interface IBookingConfirmedHandler
{
    Task HandleAsync(BookingConfirmed message, CancellationToken ct = default);
}
