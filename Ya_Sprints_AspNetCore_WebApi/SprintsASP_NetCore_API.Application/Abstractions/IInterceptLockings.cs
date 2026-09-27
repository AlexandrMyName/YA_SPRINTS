 

namespace SprintsASP_NetCore_API.Application.Abstractions;


public interface IInterceptLockings
{

    SemaphoreSlim GetOrAddByEventId(Guid eventId);
    SemaphoreSlim GetOrAddByBookingId(Guid bookingId);

}
