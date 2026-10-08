namespace Pomodoro.Domain.Achievements;

/// <summary>Estatísticas agregadas de um usuário, usadas para avaliar o catálogo de conquistas (US-53).</summary>
public sealed record AchievementStats(
    int CompletedFocusCount,
    int CompletedLongBreakCount,
    int CompletedRestCount,
    int MaxFocusInOneDay,
    int LongestStreakDays,
    int Level,
    int CompletedTasksCount);
