using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;

namespace Pomodoro.Application.Tasks.Delete;

public sealed class DeleteTaskHandler
{
    private readonly ITaskRepository _tasks;

    public DeleteTaskHandler(ITaskRepository tasks)
    {
        _tasks = tasks;
    }

    public async Task HandleAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetByIdForUserAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Tarefa não encontrada.");

        await _tasks.DeleteAsync(task, cancellationToken);
    }
}
