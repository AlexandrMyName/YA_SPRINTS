using SprintsASP_NetCore_API.Application.Abstractions;
using System.Collections.Concurrent; 


namespace SprintsASP_NetCore_API.Infrastructure.Concurrency;


public class InterceptLockings : IInterceptLockings
{

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();

    public SemaphoreSlim GetOrAddByEventId(Guid eventId)
        => _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));

    public SemaphoreSlim GetOrAddByBookingId(Guid bookingId)
        => _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
}
