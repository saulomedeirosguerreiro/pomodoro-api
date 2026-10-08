namespace Pomodoro.Application.Progress.GetProgress;

public sealed record ProgressResponse(
    int Level,
    string Title,
    int XpInLevel,
    int XpForNextLevel,
    int TotalXp,
    int Seeds,
    int StreakDays,
    bool IsStreakAtRiskToday,
    int TodayFocusCount,
    int TodayFocusSeconds);
