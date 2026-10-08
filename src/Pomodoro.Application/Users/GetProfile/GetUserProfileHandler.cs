using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Users.GetProfile;

public sealed class GetUserProfileHandler
{
    private readonly IUserRepository _users;
    private readonly IPomodoroSessionRepository _sessions;

    public GetUserProfileHandler(IUserRepository users, IPomodoroSessionRepository sessions)
    {
        _users = users;
        _sessions = sessions;
    }

    public async Task<UserProfileResponse> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        var completedSessions = await _sessions.CountCompletedFocusAsync(userId, cancellationToken);

        return new UserProfileResponse(user.Id, user.Name, user.Email, completedSessions);
    }
}
