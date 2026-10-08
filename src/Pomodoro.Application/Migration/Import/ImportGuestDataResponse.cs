namespace Pomodoro.Application.Migration.Import;

public sealed record ImportGuestDataResponse(
    int ImportId,
    string GuestId,
    DateTime ImportedAt,
    int TasksImported,
    int SessionsImported,
    IReadOnlyList<ImportedAchievementSummary> AchievementsUnlocked,
    IReadOnlyList<ImportSkippedItem> Skipped);

public sealed record ImportedAchievementSummary(string Code, string Name, DateTime UnlockedAt);

/// <summary>`ItemType`: "task" | "session".</summary>
public sealed record ImportSkippedItem(string ItemType, string LocalId, string Reason);
