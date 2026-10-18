 
using Sprints.Users.Application.Abstractions;
using System.Collections.Concurrent; 


namespace Sprints.Users.Infrastructure.Concurrency;


public class InterceptLockings : IInterceptLockings
{

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventsLocks = new();

    public SemaphoreSlim GetOrAddByBookingId(Guid bookingId)
        => _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));


    public SemaphoreSlim GetOrAddByEventId(Guid eventId)
       => _eventsLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
 
}
