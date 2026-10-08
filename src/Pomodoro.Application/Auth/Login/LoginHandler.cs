using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Auth.Login;

public sealed class LoginHandler
{
    private const string InvalidCredentialsMessage = "E-mail ou senha inválidos.";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public LoginHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokenService)
    {
        _users = users;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        var token = _tokenService.GenerateToken(user);

        return new LoginResponse(token, new LoginUserSummary(user.Id, user.Name, user.Email));
    }
}
