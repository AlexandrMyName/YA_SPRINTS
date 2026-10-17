 

namespace Sprints.Bookings.Application.Abstractions;


public interface IKafkaPublisher
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)  where T : class;
}
