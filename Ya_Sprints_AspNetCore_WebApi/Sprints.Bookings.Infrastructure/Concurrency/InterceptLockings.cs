using Sprints.Bookings.Application.Abstractions;
using System.Collections.Concurrent; 


namespace Sprints.Bookings.Infrastructure.Concurrency;


public class InterceptLockings : IInterceptLockings
{

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new(); 
     

    public SemaphoreSlim GetOrAddByBookingId(Guid bookingId)
        => _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
}
