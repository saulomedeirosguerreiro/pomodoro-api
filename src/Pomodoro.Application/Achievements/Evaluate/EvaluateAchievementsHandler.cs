using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Achievements;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Achievements.Evaluate;

/// <summary>
/// Avalia o catálogo contra as estatísticas atuais do usuário e persiste as conquistas novas (US-53 RN-02).
/// Idempotente: nunca grava a mesma conquista duas vezes, não importa quantas vezes seja chamada.
/// </summary>
public sealed class EvaluateAchievementsHandler : IAchievementEvaluator
{
    private readonly AchievementStatsProvider _statsProvider;
    private readonly IAchievementRepository _achievements;
    private readonly IDateTimeProvider _clock;

    public EvaluateAchievementsHandler(
        AchievementStatsProvider statsProvider, IAchievementRepository achievements, IDateTimeProvider clock)
    {
        _statsProvider = statsProvider;
        _achievements = achievements;
        _clock = clock;
    }

    public async Task<IReadOnlyList<string>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var stats = await _statsProvider.BuildAsync(userId, cancellationToken);
        var alreadyUnlocked = (await _achievements.ListForUserAsync(userId, cancellationToken))
            .Select(a => a.Code)
            .ToHashSet();

        var newlyUnlocked = new List<string>();
        foreach (var definition in AchievementCatalog.All)
        {
            if (alreadyUnlocked.Contains(definition.Code) || !definition.IsUnlocked(stats))
            {
                continue;
            }

            await _achievements.AddAsync(UserAchievement.Create(userId, definition.Code, _clock.UtcNow), cancellationToken);
            newlyUnlocked.Add(definition.Code);
        }

        return newlyUnlocked;
    }
}
