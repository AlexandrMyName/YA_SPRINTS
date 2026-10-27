using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.Abstractions;


namespace Sprints.Events.Infrastructure.Caching;

public sealed class NullEventCache : IEventCache
{
    public Task<EventInfoDto?> GetEventAsync(Guid id, CancellationToken ct = default) => Task.FromResult<EventInfoDto?>(null);
    public Task SetEventAsync(EventInfoDto evt, CancellationToken ct = default) => Task.CompletedTask;
    public Task InvalidateEventAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<IReadOnlyList<EventInfoDto>?> GetTopEventsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EventInfoDto>?>(null);
    public Task SetTopEventsAsync(IReadOnlyList<EventInfoDto> events, CancellationToken ct = default) => Task.CompletedTask;
    public Task InvalidateTopEventsAsync(CancellationToken ct = default) => Task.CompletedTask;
}