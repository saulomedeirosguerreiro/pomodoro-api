using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> FindByHashAsync(string hash, CancellationToken cancellationToken);

    /// <summary>Revoga todos os refresh tokens ativos do usuário — reuso detectado, troca de senha ou exclusão de conta.</summary>
    Task RevokeAllForUserAsync(int userId, DateTime utcNow, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
