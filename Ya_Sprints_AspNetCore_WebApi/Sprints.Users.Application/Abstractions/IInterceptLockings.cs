

namespace Sprints.Users.Application.Abstractions;


public interface IInterceptLockings
{

    SemaphoreSlim GetOrAddByEventId(Guid eventId);
    SemaphoreSlim GetOrAddByBookingId(Guid bookingId);

}
