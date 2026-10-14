using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Auth.Refresh;

/// <summary>
/// Rotação de refresh token: a cada chamada o token recebido é revogado e um novo é emitido.
/// Reapresentar um token já revogado é sinal de roubo (reuso) — derruba TODAS as sessões do usuário,
/// não só a chamada atual.
/// </summary>
public sealed class RefreshTokenHandler
{
    private const string InvalidRefreshTokenMessage = "Sessão expirada. Faça login novamente.";

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _clock;

    public RefreshTokenHandler(
        IRefreshTokenRepository refreshTokens, IUserRepository users, ITokenService tokenService, IDateTimeProvider clock)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<RefreshResult> HandleAsync(string rawRefreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _refreshTokens.FindByHashAsync(hash, cancellationToken)
            ?? throw new InvalidCredentialsException(InvalidRefreshTokenMessage);

        if (existing.RevokedAt is not null)
        {
            await _refreshTokens.RevokeAllForUserAsync(existing.UserId, _clock.UtcNow, cancellationToken);
            throw new InvalidCredentialsException(InvalidRefreshTokenMessage);
        }

        if (!existing.IsActive(_clock.UtcNow))
        {
            throw new InvalidCredentialsException(InvalidRefreshTokenMessage);
        }

        var user = await _users.FindByIdAsync(existing.UserId, cancellationToken)
            ?? throw new InvalidCredentialsException(InvalidRefreshTokenMessage);

        var newRefreshTokenData = _tokenService.GenerateRefreshToken(_clock.UtcNow);
        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenData.Hash, _clock.UtcNow, newRefreshTokenData.ExpiresAt);

        existing.Revoke(_clock.UtcNow, newRefreshTokenData.Hash);
        await _refreshTokens.AddAsync(newRefreshToken, cancellationToken);

        var accessToken = _tokenService.GenerateToken(user);

        return new RefreshResult(accessToken, newRefreshTokenData.RawToken, newRefreshTokenData.ExpiresAt);
    }
}
