using Sprints.Events.Application.Dtos.EntitiesDtos.Events;


namespace Sprints.Events.Application.Abstractions;


public interface IEventCache
{
    Task<EventInfoDto?> GetEventAsync(Guid id, CancellationToken ct = default);
    Task SetEventAsync(EventInfoDto evt, CancellationToken ct = default);
    Task InvalidateEventAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EventInfoDto>?> GetTopEventsAsync(CancellationToken ct = default);
    Task SetTopEventsAsync(IReadOnlyList<EventInfoDto> events, CancellationToken ct = default);
    Task InvalidateTopEventsAsync(CancellationToken ct = default);
}
