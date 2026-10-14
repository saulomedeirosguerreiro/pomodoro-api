using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Users.ChangePassword;

/// <summary>Tela de Perfil — exige a senha atual, mesmo padrão de <see cref="Users.Delete.DeleteAccountHandler"/>.</summary>
public sealed class ChangePasswordHandler
{
    private const string InvalidPasswordMessage = "Senha atual inválida.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly IRefreshTokenRepository _refreshTokens;

    public ChangePasswordHandler(
        IUserRepository users, IPasswordHasher hasher, IDateTimeProvider clock, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _hasher = hasher;
        _clock = clock;
        _refreshTokens = refreshTokens;
    }

    public async Task HandleAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidPasswordMessage);
        }

        user.UpdatePassword(_hasher.Hash(request.NewPassword), _clock.UtcNow);
        await _users.SaveChangesAsync(cancellationToken);

        // Troca de senha derruba todas as outras sessões — mitiga o caso de um refresh token já vazado.
        await _refreshTokens.RevokeAllForUserAsync(userId, _clock.UtcNow, cancellationToken);
    }
}
