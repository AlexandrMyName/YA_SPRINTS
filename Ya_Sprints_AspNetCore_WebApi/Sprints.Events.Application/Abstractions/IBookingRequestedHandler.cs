using Sprints.Contracts; 


namespace Sprints.Events.Application.Abstractions;

public interface IBookingRequestedHandler
{
    Task HandleAsync(BookingRequested message, CancellationToken ct = default);
}
