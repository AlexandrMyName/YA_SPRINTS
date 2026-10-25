using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Microsoft.Extensions.Caching.Distributed;
using Sprints.Events.Application.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;


namespace Sprints.Events.Infrastructure.Caching;


public sealed class RedisEventCache : IEventCache
{

    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;

    public RedisEventCache(IDistributedCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<EventInfoDto?> GetEventAsync(Guid id, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(CacheKeys.Event(id), ct);
        return bytes is null ? null : JsonSerializer.Deserialize<EventInfoDto>(bytes);
    }

    public Task SetEventAsync(EventInfoDto evt, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(evt);
        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.EventTtlSeconds)
        };
        return _cache.SetAsync(CacheKeys.Event(evt.Id), bytes, opts, ct);
    }

    public Task InvalidateEventAsync(Guid id, CancellationToken ct = default)
        => _cache.RemoveAsync(CacheKeys.Event(id), ct);

    public async Task<IReadOnlyList<EventInfoDto>?> GetTopEventsAsync(CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(CacheKeys.TopEvents, ct);
        return bytes is null
            ? null
            : JsonSerializer.Deserialize<List<EventInfoDto>>(bytes);
    }

    public Task SetTopEventsAsync(IReadOnlyList<EventInfoDto> events, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(events);
        var opts = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.TopEventsTtlSeconds)
        };
        return _cache.SetAsync(CacheKeys.TopEvents, bytes, opts, ct);
    }

    public Task InvalidateTopEventsAsync(CancellationToken ct = default)
        => _cache.RemoveAsync(CacheKeys.TopEvents, ct);
}
