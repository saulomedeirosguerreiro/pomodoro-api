using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Tasks.SetStatus;

public sealed class SetTaskStatusHandler
{
    private readonly ITaskRepository _tasks;
    private readonly IPomodoroSessionRepository _sessions;
    private readonly IDateTimeProvider _clock;
    private readonly IAchievementEvaluator _achievementEvaluator;

    public SetTaskStatusHandler(
        ITaskRepository tasks, IPomodoroSessionRepository sessions, IDateTimeProvider clock,
        IAchievementEvaluator achievementEvaluator)
    {
        _tasks = tasks;
        _sessions = sessions;
        _clock = clock;
        _achievementEvaluator = achievementEvaluator;
    }

    public async Task<TaskResponse> HandleAsync(
        int id, int userId, SetTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdForUserAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Tarefa não encontrada.");

        TaskWireFormat.TryParseStatus(request.Status, out var status);

        switch (status)
        {
            case TaskItemStatus.EmCurso:
                // G-Q13a: no máximo uma tarefa em foco por usuário — a anterior sai do foco.
                var currentlyFocused = await _tasks.FindInFocusAsync(userId, cancellationToken);
                if (currentlyFocused is not null && !ReferenceEquals(currentlyFocused, task))
                {
                    currentlyFocused.MarkAsTodo(_clock.UtcNow);
                }

                task.MarkAsFocused(_clock.UtcNow);
                break;
            case TaskItemStatus.Feito:
                task.MarkAsDone(_clock.UtcNow);
                break;
            default:
                task.MarkAsTodo(_clock.UtcNow);
                break;
        }

        await _tasks.SaveChangesAsync(cancellationToken);

        if (status == TaskItemStatus.Feito)
        {
            // US-53 RN-02: concluir uma tarefa também pode destravar uma conquista (ex.: Primeira Colheita).
            await _achievementEvaluator.HandleAsync(userId, cancellationToken);
        }

        var completedByTask = await _sessions.CountCompletedByTaskAsync([task.Id], cancellationToken);

        return TaskResponse.FromDomain(task, completedByTask.GetValueOrDefault(task.Id));
    }
}
