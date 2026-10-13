 

namespace SprintASP_NetCore_API.Domain.Entities;


public class RefreshToken : IEntity
{

    public Guid Id { get; set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    private RefreshToken() { }

    public static RefreshToken Create(Guid id, Guid userId, string tokenHash, TimeSpan lifetime)
        => new()
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            CreatedAt = DateTime.UtcNow
        };

    public void Revoke(Guid? replacedByTokenId = null)
    {
        if (RevokedAt != null) return;
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }
}