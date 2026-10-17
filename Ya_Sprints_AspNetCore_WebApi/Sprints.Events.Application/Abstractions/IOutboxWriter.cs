

namespace Sprints.Events.Application.Abstractions;

public interface IOutboxWriter
{
    Task EnqueueAsync<T>(string topic, string key, T payload, CancellationToken ct = default)
        where T : class;
}
