namespace Pomodoro.Application.Tasks.Create;

/// <summary>`UserId` nunca é aceito aqui: o dono da tarefa vem sempre do token (L-13).</summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    string Priority,
    int EstimatedPomodoros);
