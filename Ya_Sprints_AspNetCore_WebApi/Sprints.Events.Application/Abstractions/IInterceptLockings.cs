

namespace Sprints.Events.Application.Abstractions;


public interface IInterceptLockings
{

    SemaphoreSlim GetOrAddByEventId(Guid eventId); 

}
