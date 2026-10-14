using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);

    /// <summary>Gera um refresh token opaco. O hash é o único valor persistido (ver <see cref="HashRefreshToken"/>).</summary>
    RefreshTokenData GenerateRefreshToken(DateTime utcNow);

    /// <summary>Hash determinístico usado tanto para persistir quanto para localizar um refresh token pelo valor bruto recebido.</summary>
    string HashRefreshToken(string rawToken);
}

public sealed record RefreshTokenData(string RawToken, string Hash, DateTime ExpiresAt);
