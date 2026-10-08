namespace Pomodoro.Application.Achievements;

public sealed record AchievementResponse(
    string Code,
    string Name,
    string Description,
    DateTime? UnlockedAt,
    int? ProgressCurrent,
    int? ProgressTarget);
