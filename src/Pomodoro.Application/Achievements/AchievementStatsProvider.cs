using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Achievements;
using Pomodoro.Domain.Enums;
using Pomodoro.Domain.Services;

namespace Pomodoro.Application.Achievements;

/// <summary>Monta <see cref="AchievementStats"/> para um usuário a partir dos repositórios (I/O).</summary>
public sealed class AchievementStatsProvider
{
    private readonly IPomodoroSessionRepository _sessions;
    private readonly ITaskRepository _tasks;

    public AchievementStatsProvider(IPomodoroSessionRepository sessions, ITaskRepository tasks)
    {
        _sessions = sessions;
        _tasks = tasks;
    }

    public async Task<AchievementStats> BuildAsync(int userId, CancellationToken cancellationToken)
    {
        var sessions = await _sessions.ListAllAsync(userId, cancellationToken);
        var completedTasks = await _tasks.ListForUserAsync(userId, TaskItemStatus.Feito, cancellationToken);

        var totalXp = sessions.Sum(s => ProgressRules.XpFor(s.Type, s.Status));
        var (level, _, _) = ProgressRules.CalculateLevel(totalXp);

        return AchievementCalculator.BuildStats(sessions, completedTasks.Count, level);
    }
}
