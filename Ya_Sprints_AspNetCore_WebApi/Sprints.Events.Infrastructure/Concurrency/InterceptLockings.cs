using Sprints.Events.Application.Abstractions;
using System.Collections.Concurrent; 


namespace Sprints.Events.Infrastructure.Concurrency;


public class InterceptLockings : IInterceptLockings
{
     
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();

    public SemaphoreSlim GetOrAddByEventId(Guid eventId)
        => _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
     
}
