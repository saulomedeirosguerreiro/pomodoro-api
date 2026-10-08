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
            await EnsureTaskCanBeLinkedAsync(userId, request.TaskId.Value, type, cancellationToken);
        }

        var session = PomodoroSession.Create(
            userId, type, status, request.DurationSeconds, request.StartedAt, request.CompletedAt, _clock.UtcNow,
            request.TaskId);

        await _sessions.AddAsync(session, cancellationToken);

        // US-53 RN-02: toda sessão registrada pode destravar uma conquista nova.
        await _achievementEvaluator.HandleAsync(userId, cancellationToken);

        return PomodoroSessionResponse.FromDomain(session);
    }

    /// <summary>
    /// US-50: só aceita o vínculo quando a tarefa é do mesmo usuário, a sessão é de foco e a tarefa não está concluída.
    /// Mensagem genérica em qualquer um desses casos, para não revelar a existência de tarefas de outros usuários (L-11).
    /// </summary>
    private async Task EnsureTaskCanBeLinkedAsync(
        int userId, int taskId, SessionType type, CancellationToken cancellationToken)
    {
        const string message = "Tarefa não encontrada ou indisponível para vínculo.";

        var task = await _tasks.GetByIdForUserAsync(taskId, userId, cancellationToken);
        if (task is null || type != SessionType.Foco || task.Status == TaskItemStatus.Feito)
        {
            throw new FieldValidationException("TaskId", message);
        }
    }
}
