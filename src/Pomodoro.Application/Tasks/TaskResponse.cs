using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Tasks;

public sealed record TaskResponse(
    int Id,
    string Title,
    string? Description,
    string Priority,
    int EstimatedPomodoros,
    int CompletedPomodoros,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static TaskResponse FromDomain(TaskItem task, int completedPomodoros) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Priority.ToWire(),
        task.EstimatedPomodoros,
        completedPomodoros,
        task.Status.ToWire(),
        task.CreatedAt,
        task.UpdatedAt);
}
