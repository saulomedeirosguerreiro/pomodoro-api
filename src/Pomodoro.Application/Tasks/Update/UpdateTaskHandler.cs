using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Tasks.Update;

public sealed class UpdateTaskHandler
{
    private readonly ITaskRepository _tasks;
    private readonly IPomodoroSessionRepository _sessions;
    private readonly IDateTimeProvider _clock;

    public UpdateTaskHandler(ITaskRepository tasks, IPomodoroSessionRepository sessions, IDateTimeProvider clock)
    {
        _tasks = tasks;
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<TaskResponse> HandleAsync(
        int id, int userId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdForUserAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Tarefa não encontrada.");

        TaskWireFormat.TryParsePriority(request.Priority, out var priority);

        task.UpdateDetails(request.Title, request.Description, priority, request.EstimatedPomodoros, _clock.UtcNow);

        await _tasks.SaveChangesAsync(cancellationToken);

        var completedByTask = await _sessions.CountCompletedByTaskAsync([task.Id], cancellationToken);

        return TaskResponse.FromDomain(task, completedByTask.GetValueOrDefault(task.Id));
    }
}
