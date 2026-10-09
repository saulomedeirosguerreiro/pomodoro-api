using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Pomodoros;

public sealed record PomodoroSessionResponse(
    int Id,
    string Type,
    string Status,
    int DurationSeconds,
    DateTime StartedAt,
    DateTime CompletedAt,
    DateTime CreatedAt,
    string? Mode = null,
    int? PlannedDurationSeconds = null,
    int? AddedSeconds = null)
{
    public static PomodoroSessionResponse FromDomain(PomodoroSession session) => new(
        session.Id,
        session.Type.ToWire(),
        session.Status.ToWire(),
        session.DurationSeconds,
        session.StartedAt,
        session.CompletedAt,
        session.CreatedAt,
        session.Mode,
        session.PlannedDurationSeconds,
        session.AddedSeconds);
}
