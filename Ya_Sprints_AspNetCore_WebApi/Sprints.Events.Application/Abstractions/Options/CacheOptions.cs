

namespace Sprints.Events.Application.Abstractions.Options;


public sealed class CacheOptions
{
    public int EventTtlSeconds { get; set; } = 300;
    public int TopEventsTtlSeconds { get; set; } = 60;
}
