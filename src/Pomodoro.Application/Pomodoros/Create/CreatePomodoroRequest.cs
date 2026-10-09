namespace Pomodoro.Application.Pomodoros.Create;

/// <summary>
/// `UserId` nunca é aceito aqui de propósito: o dono da sessão vem sempre do token (L-13),
/// nunca do corpo da requisição.
/// </summary>
public sealed record CreatePomodoroRequest(
    string Type,
    string Status,
    int DurationSeconds,
    DateTime StartedAt,
    DateTime CompletedAt,
    int? TaskId = null,
    string? Mode = null,
    int? PlannedDurationSeconds = null,
    int? AddedSeconds = null);
