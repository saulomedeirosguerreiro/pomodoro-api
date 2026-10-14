using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Auth.Login;

public sealed class LoginHandler
{
    private const string InvalidCredentialsMessage = "E-mail ou senha inválidos.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;

    public LoginHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IDateTimeProvider clock)
    {
        _users = users;
        _hasher = hasher;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<LoginResult> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        var accessToken = _tokenService.GenerateToken(user);

        var refreshTokenData = _tokenService.GenerateRefreshToken(_clock.UtcNow);
        await _refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, refreshTokenData.Hash, _clock.UtcNow, refreshTokenData.ExpiresAt),
            cancellationToken);

        return new LoginResult(
            accessToken,
            refreshTokenData.RawToken,
            refreshTokenData.ExpiresAt,
            new LoginUserSummary(user.Id, user.Name, user.Email));
    }
}
