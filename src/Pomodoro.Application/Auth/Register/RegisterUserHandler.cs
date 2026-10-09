using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Auth.Register;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public RegisterUserHandler(IUserRepository users, IPasswordHasher hasher, IDateTimeProvider clock)
    {
        _users = users;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<RegisterUserResponse> HandleAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        if (await _users.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("E-mail já cadastrado.");
        }

        var passwordHash = _hasher.Hash(request.Password);
        var user = User.Create(request.Name, request.Email, passwordHash, _clock.UtcNow, termsAcceptedAt: _clock.UtcNow);

        await _users.AddAsync(user, cancellationToken);

        return new RegisterUserResponse(user.Id, user.Name, user.Email);
    }
}
