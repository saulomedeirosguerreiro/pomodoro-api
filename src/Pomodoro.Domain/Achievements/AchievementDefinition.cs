namespace Pomodoro.Domain.Achievements;

public sealed record AchievementProgress(int Current, int Target);

public sealed record AchievementDefinition(
    string Code,
    string Name,
    string Description,
    Func<AchievementStats, bool> IsUnlocked,
    Func<AchievementStats, AchievementProgress?> Progress);
