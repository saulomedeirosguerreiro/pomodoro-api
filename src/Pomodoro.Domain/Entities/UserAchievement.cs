using Pomodoro.Domain.Common;

namespace Pomodoro.Domain.Entities;

/// <summary>Registro de uma conquista desbloqueada por um usuário (US-53 RN-02) — nunca duplica por (UserId, Code).</summary>
public sealed class UserAchievement
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateTime UnlockedAt { get; private set; }

    private UserAchievement()
    {
    }

    public static UserAchievement Create(int userId, string code, DateTime utcNow)
    {
        if (userId <= 0)
        {
            throw new DomainException("Conquista precisa pertencer a um usuário válido.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Código da conquista é obrigatório.");
        }

        return new UserAchievement
        {
            UserId = userId,
            Code = code,
            UnlockedAt = utcNow,
        };
    }
}
