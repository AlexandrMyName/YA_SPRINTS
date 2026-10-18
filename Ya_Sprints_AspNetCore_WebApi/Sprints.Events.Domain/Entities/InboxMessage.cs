 

namespace Sprints.Events.Domain.Entities;

/// <summary>
/// Обработанное сообщение — для дедупликации при повторной доставке.
/// </summary>
public class InboxMessage : IEntity
{
    public Guid Id { get; set; }           // = MessageId из Kafka
    public string Type { get; private set; } = "";
    public DateTime ReceivedAt { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private InboxMessage() { }

    public static InboxMessage Create(Guid messageId, string type)
        => new()
        {
            Id = messageId,
            Type = type,
            ReceivedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };
}