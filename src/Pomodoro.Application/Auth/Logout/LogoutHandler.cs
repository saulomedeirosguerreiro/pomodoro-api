using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;

namespace Pomodoro.Application.Auth.Logout;

/// <summary>
/// Revoga o refresh token no servidor — sem isso, logout seria só client-side (como antes do refresh
/// token existir) e o cookie continuaria válido até a expiração natural mesmo após "sair".
/// Idempotente: token ausente/já revogado/inexistente não é erro, só não faz nada.
/// </summary>
public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _clock;

    public LogoutHandler(IRefreshTokenRepository refreshTokens, ITokenService tokenService, IDateTimeProvider clock)
    {
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task HandleAsync(string? rawRefreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var hash = _tokenService.HashRefreshToken(rawRefreshToken);
        var existing = await _refreshTokens.FindByHashAsync(hash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null)
        {
            return;
        }

        existing.Revoke(_clock.UtcNow);
        await _refreshTokens.SaveChangesAsync(cancellationToken);
    }
}
