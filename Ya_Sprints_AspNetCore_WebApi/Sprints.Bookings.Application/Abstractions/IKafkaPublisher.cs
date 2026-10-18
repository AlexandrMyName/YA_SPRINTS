 

namespace Sprints.Bookings.Application.Abstractions;


public interface IKafkaPublisher
{  
    
    /// <summary>
   /// Сериализует объект в JSON и отправляет в Kafka 
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="topic"></param>
   /// <param name="key"></param>
   /// <param name="message"></param>
   /// <param name="ct"></param>
   /// <returns></returns>
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Отправляет уже сериализованную JSON-строку (для Outbox, где payload уже готов)
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="key"></param>
    /// <param name="rawJson"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task PublishRawAsync(string topic, string key, string rawJson, CancellationToken ct = default);
}