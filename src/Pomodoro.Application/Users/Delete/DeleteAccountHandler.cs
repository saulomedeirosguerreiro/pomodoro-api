using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Users.Delete;

/// <summary>Exclusão definitiva da conta: exige a senha atual, apaga o usuário e tudo que depende dele.</summary>
public sealed class DeleteAccountHandler
{
    private const string InvalidPasswordMessage = "Senha inválida.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public DeleteAccountHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task HandleAsync(int userId, DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidPasswordMessage);
        }

        await _users.DeleteAsync(user, cancellationToken);
    }
}
