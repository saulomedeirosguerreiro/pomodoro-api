using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Pomodoros.Create;

public sealed class CreatePomodoroHandler
{
    private readonly IPomodoroSessionRepository _sessions;
    private readonly ITaskRepository _tasks;
    private readonly IDateTimeProvider _clock;
    private readonly IAchievementEvaluator _achievementEvaluator;

    public CreatePomodoroHandler(
        IPomodoroSessionRepository sessions, ITaskRepository tasks, IDateTimeProvider clock,
        IAchievementEvaluator achievementEvaluator)
    {
        _sessions = sessions;
        _tasks = tasks;
        _clock = clock;
        _achievementEvaluator = achievementEvaluator;
    }

    public async Task<PomodoroSessionResponse> HandleAsync(
        int userId, CreatePomodoroRequest request, CancellationToken cancellationToken)
    {
        SessionWireFormat.TryParseType(request.Type, out var type);
        SessionWireFormat.TryParseStatus(request.Status, out var status);

        // US-41: duas sessões do mesmo usuário nunca podem se sobrepor no tempo (antifraude).
        var overlaps = await _sessions.ExistsOverlappingAsync(
            userId, request.StartedAt, request.CompletedAt, cancellationToken);
        if (overlaps)
        {
            throw new FieldValidationException("StartedAt", "Esta sessão se sobrepõe a outra já registrada.");
        }

        if (request.TaskId.HasValue)
        {
            await TaskLinkGuard.EnsureCanBeLinkedAsync(_tasks, userId, request.TaskId.Value, type, cancellationToken);
        }

        var session = PomodoroSession.Create(
            userId, type, status, request.DurationSeconds, request.StartedAt, request.CompletedAt, _clock.UtcNow,
            request.TaskId);

        await _sessions.AddAsync(session, cancellationToken);

        // US-53 RN-02: toda sessão registrada pode destravar uma conquista nova.
        await _achievementEvaluator.HandleAsync(userId, cancellationToken);

        return PomodoroSessionResponse.FromDomain(session);
    }
}
