

namespace Sprints.Events.Application.Abstractions.Options;


public static class CacheKeys
{
    public static string Event(Guid id) => $"events:event:{id}";
    public const string TopEvents = "events:top10";
}
