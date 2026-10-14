namespace Pomodoro.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public int UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Hash do token que substituiu este na rotação — rastreia a cadeia para auditoria.</summary>
    public string? ReplacedByTokenHash { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(int userId, string tokenHash, DateTime utcNow, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = utcNow,
            ExpiresAt = expiresAt,
        };
    }

    public void Revoke(DateTime utcNow, string? replacedByTokenHash = null)
    {
        RevokedAt = utcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
