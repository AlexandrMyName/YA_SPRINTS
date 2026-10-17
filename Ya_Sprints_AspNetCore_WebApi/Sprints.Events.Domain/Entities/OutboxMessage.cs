 

namespace Sprints.Events.Domain.Entities;

/// <summary>
/// Сообщение, ожидающее отправки в Kafka.
/// Сохраняется в БД в одной транзакции с бизнес-изменениями.
/// </summary>
public class OutboxMessage : IEntity
{
    public Guid Id { get; set; }           // = MessageId (уходит в Kafka)
    public string Type { get; private set; } = ""; // "BookingConfirmed"
    public string Topic { get; private set; } = "";
    public string Key { get; private set; } = "";
    public string Payload { get; private set; } = ""; // JSON
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        Guid messageId, string type, string topic, string key, string payload)
        => new()
        {
            Id = messageId,
            Type = type,
            Topic = topic,
            Key = key,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0
        };

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error;
    }
}