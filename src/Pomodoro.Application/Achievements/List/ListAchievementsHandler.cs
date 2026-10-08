using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Achievements;

namespace Pomodoro.Application.Achievements.List;

/// <summary>Catálogo completo + status do usuário (US-53 RN-03), avaliando retroativamente antes de responder.</summary>
public sealed class ListAchievementsHandler
{
    private readonly IAchievementEvaluator _evaluator;
    private readonly AchievementStatsProvider _statsProvider;
    private readonly IAchievementRepository _achievements;

    public ListAchievementsHandler(
        IAchievementEvaluator evaluator, AchievementStatsProvider statsProvider, IAchievementRepository achievements)
    {
        _evaluator = evaluator;
        _statsProvider = statsProvider;
        _achievements = achievements;
    }

    public async Task<IReadOnlyList<AchievementResponse>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        await _evaluator.HandleAsync(userId, cancellationToken);

        var stats = await _statsProvider.BuildAsync(userId, cancellationToken);
        var unlockedAtByCode = (await _achievements.ListForUserAsync(userId, cancellationToken))
            .ToDictionary(a => a.Code, a => a.UnlockedAt);

        return AchievementCatalog.All
            .Select(definition =>
            {
                var unlockedAt = unlockedAtByCode.TryGetValue(definition.Code, out var at) ? at : (DateTime?)null;
                var progress = unlockedAt is null ? definition.Progress(stats) : null;

                return new AchievementResponse(
                    definition.Code, definition.Name, definition.Description,
                    unlockedAt, progress?.Current, progress?.Target);
            })
            .ToList();
    }
}
