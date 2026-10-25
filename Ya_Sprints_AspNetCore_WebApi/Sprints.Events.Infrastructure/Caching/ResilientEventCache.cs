using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.Abstractions;
using Microsoft.Extensions.Logging;


namespace Sprints.Events.Infrastructure.Caching;

/// <summary>
/// Декоратор над <see cref="IEventCache"/>: гарантирует, что недоступность Redis
/// не приводит к ошибкам у клиента — деградирует в cache miss (get) и no-op (set/invalidate).
/// </summary>
public sealed class ResilientEventCache : IEventCache
{

    private readonly IEventCache _inner;
    private readonly ILogger<ResilientEventCache> _logger;

    public ResilientEventCache(IEventCache inner, ILogger<ResilientEventCache> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    
    public async Task<EventInfoDto?> GetEventAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _inner.GetEventAsync(id, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache get failed for event {EventId}. Falling back to DB.", id);
            return null; // трактуем как cache miss — хендлер пойдёт в БД
        }
    }

    public async Task SetEventAsync(EventInfoDto evt, CancellationToken ct = default)
    {
        try
        {
            await _inner.SetEventAsync(evt, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache set failed for event {EventId}.", evt.Id);
        }
    }

    public async Task InvalidateEventAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _inner.InvalidateEventAsync(id, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache invalidate failed for event {EventId}. Will expire by TTL.", id);
        }
    }

   
    public async Task<IReadOnlyList<EventInfoDto>?> GetTopEventsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _inner.GetTopEventsAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache get failed for top events. Falling back to DB.");
            return null; // cache miss → хендлер пойдёт в БД
        }
    }

    public async Task SetTopEventsAsync(IReadOnlyList<EventInfoDto> events, CancellationToken ct = default)
    {
        try
        {
            await _inner.SetTopEventsAsync(events, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache set failed for top events.");
        }
    }

    public async Task InvalidateTopEventsAsync(CancellationToken ct = default)
    {
        try
        {
            await _inner.InvalidateTopEventsAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cache invalidate failed for top events. Will expire by TTL.");
        }
    }
}