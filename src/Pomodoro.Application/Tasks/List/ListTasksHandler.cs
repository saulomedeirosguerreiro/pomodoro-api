using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Tasks.List;

public sealed class ListTasksHandler
{
    private readonly ITaskRepository _tasks;
    private readonly IPomodoroSessionRepository _sessions;

    public ListTasksHandler(ITaskRepository tasks, IPomodoroSessionRepository sessions)
    {
        _tasks = tasks;
        _sessions = sessions;
    }

    public async Task<IReadOnlyList<TaskResponse>> HandleAsync(
        int userId, string? status, CancellationToken cancellationToken)
    {
        Domain.Enums.TaskItemStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TaskWireFormat.TryParseStatus(status, out var value))
            {
                throw new FieldValidationException("status", "Status de tarefa inválido. Use a_fazer, em_curso ou feito.");
            }

            parsedStatus = value;
        }

        var items = await _tasks.ListForUserAsync(userId, parsedStatus, cancellationToken);

        var completedByTask = await _sessions.CountCompletedByTaskAsync(
            items.Select(t => t.Id).ToList(), cancellationToken);

        return items
            .Select(t => TaskResponse.FromDomain(t, completedByTask.GetValueOrDefault(t.Id)))
            .ToList();
    }
}
