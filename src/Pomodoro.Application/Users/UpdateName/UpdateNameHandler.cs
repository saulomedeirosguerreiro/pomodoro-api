using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Users.GetProfile;

namespace Pomodoro.Application.Users.UpdateName;

public sealed class UpdateNameHandler
{
    private readonly IUserRepository _users;
    private readonly IPomodoroSessionRepository _sessions;
    private readonly IDateTimeProvider _clock;

    public UpdateNameHandler(IUserRepository users, IPomodoroSessionRepository sessions, IDateTimeProvider clock)
    {
        _users = users;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<UserProfileResponse> HandleAsync(int userId, UpdateNameRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        user.Rename(request.Name, _clock.UtcNow);
        await _users.SaveChangesAsync(cancellationToken);

        var completedSessions = await _sessions.CountCompletedFocusAsync(userId, cancellationToken);

        return new UserProfileResponse(user.Id, user.Name, user.Email, completedSessions);
    }
}
