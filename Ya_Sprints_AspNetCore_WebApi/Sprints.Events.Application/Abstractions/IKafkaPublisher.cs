using System.Threading;
using System.Threading.Tasks;

namespace Sprints.Events.Application.Abstractions;

public interface IKafkaPublisher
{
    Task PublishAsync<T>(string topic, string key, T message, string? traceParent = null, CancellationToken ct = default)
        where T : class;

    Task PublishRawAsync(string topic, string key, string rawJson, string? traceParent = null, CancellationToken ct = default);
}