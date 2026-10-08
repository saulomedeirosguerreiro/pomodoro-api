using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Auth.RecoverPassword;

/// <summary>
/// Troca a senha sem e-mail/token: exige nome + e-mail batendo com um usuário existente.
/// Mensagem de erro sempre a MESMA (<see cref="GenericMessage"/>), independente de o e-mail não
/// existir ou de o nome não bater — não revela qual dos dois falhou (mesmo espírito de
/// <see cref="Pomodoros.TaskLinkGuard"/>/<see cref="InvalidCredentialsException"/>).
/// </summary>
public sealed class RecoverPasswordHandler
{
    public const string GenericMessage = "Não foi possível localizar uma conta com esses dados.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public RecoverPasswordHandler(IUserRepository users, IPasswordHasher hasher, IDateTimeProvider clock)
    {
        _users = users;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task HandleAsync(RecoverPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null || !NameMatches(user.Name, request.Name))
        {
            throw new NotFoundException(GenericMessage);
        }

        user.UpdatePassword(_hasher.Hash(request.NewPassword), _clock.UtcNow);
        await _users.SaveChangesAsync(cancellationToken);
    }

    private static bool NameMatches(string storedName, string providedName) =>
        string.Equals(storedName.Trim(), providedName.Trim(), StringComparison.OrdinalIgnoreCase);
}
