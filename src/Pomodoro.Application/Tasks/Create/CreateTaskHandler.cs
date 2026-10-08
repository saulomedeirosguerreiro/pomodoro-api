using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Tasks.Create;

public sealed class CreateTaskHandler
{
    private readonly ITaskRepository _tasks;
    private readonly IDateTimeProvider _clock;

    public CreateTaskHandler(ITaskRepository tasks, IDateTimeProvider clock)
    {
        _tasks = tasks;
        _clock = clock;
    }

    public async Task<TaskResponse> HandleAsync(int userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        TaskWireFormat.TryParsePriority(request.Priority, out var priority);

        var task = TaskItem.Create(
            userId, request.Title, request.Description, priority, request.EstimatedPomodoros, _clock.UtcNow);

        await _tasks.AddAsync(task, cancellationToken);

        return TaskResponse.FromDomain(task, completedPomodoros: 0);
    }
}
