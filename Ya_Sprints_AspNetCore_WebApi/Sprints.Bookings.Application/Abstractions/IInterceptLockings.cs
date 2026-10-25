

namespace Sprints.Bookings.Application.Abstractions;


public interface IInterceptLockings
{
     
    SemaphoreSlim GetOrAddByBookingId(Guid bookingId);

}
