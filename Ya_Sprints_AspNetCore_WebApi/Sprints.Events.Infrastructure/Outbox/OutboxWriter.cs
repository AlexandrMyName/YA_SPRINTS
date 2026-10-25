using Sprints.Events.Infrastructure.DataAccess.DbContexts;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Sprints.Events.Infrastructure.Outbox;


public class OutboxWriter : IOutboxWriter
{

    private readonly AppDbContext _db;
    private readonly ILogger<OutboxWriter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxWriter(AppDbContext db, ILogger<OutboxWriter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnqueueAsync<T>(
        string topic, string key, T payload, CancellationToken ct = default)
        where T : class
    {
        // MessageId генерируется ЗДЕСЬ и уходит в payload — Inbox на той стороне
        // будет дедуплицировать по нему.
        var messageId = Guid.NewGuid();

        // Если у контракта есть MessageId — заполняем. Иначе используем анонимный прокси. 
        object payloadWithId = payload switch
        {
            Sprints.Contracts.BookingAccepted ba => ba with { MessageId = messageId },
            Sprints.Contracts.BookingRejected br => br with { MessageId = messageId },
            _ => throw new InvalidOperationException(
                $"Events.OutboxWriter: тип {typeof(T).Name} не поддерживает MessageId")
        };
        var json = JsonSerializer.Serialize(payloadWithId, JsonOptions);

        var message = OutboxMessage.Create(
            messageId: messageId,
            type: typeof(T).Name,
            topic: topic,
            key: key,
            payload: json);

        await _db.OutboxMessages.AddAsync(message, ct);

        _logger.LogDebug(
            "Outbox: добавлено сообщение {MessageId} ({Type}) в {Topic}",
            messageId, typeof(T).Name, topic);
    }
}