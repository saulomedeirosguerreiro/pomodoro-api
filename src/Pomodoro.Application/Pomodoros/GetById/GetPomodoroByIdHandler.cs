using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Pomodoros.GetById;

public sealed class GetPomodoroByIdHandler
{
    private readonly IPomodoroSessionRepository _sessions;

    public GetPomodoroByIdHandler(IPomodoroSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<PomodoroSessionResponse> HandleAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdForUserAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Sessão não encontrada.");

        return PomodoroSessionResponse.FromDomain(session);
    }
}
