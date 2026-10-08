namespace Pomodoro.Application.Tasks.Update;

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    string Priority,
    int EstimatedPomodoros);
