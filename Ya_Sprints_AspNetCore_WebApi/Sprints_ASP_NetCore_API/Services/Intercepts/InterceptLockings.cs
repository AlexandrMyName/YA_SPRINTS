using System.Collections.Concurrent;

namespace SprintASP_NetCore_API.Services.Intercepts;

public class InterceptLockings : IInterceptLockings
{


    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _bookingLocks = new(); 
    
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new();
      

    public SemaphoreSlim GetOrAddByEventId(Guid eventId) => _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
    public SemaphoreSlim GetOrAddByBookingId(Guid bookingId) => _bookingLocks.GetOrAdd(bookingId, _ => new SemaphoreSlim(1, 1));
}


public interface IInterceptLockings
{  

    SemaphoreSlim GetOrAddByEventId(Guid eventId);
    SemaphoreSlim GetOrAddByBookingId(Guid bookingId);

}

public static class AddInterceptLockingsExtention
{

    /// <summary>
    /// Добавляет коллекции синхронизации
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddInterceptLockings(this IServiceCollection services)
    {
        services.AddSingleton<IInterceptLockings, InterceptLockings>();
        return services;
    }
}