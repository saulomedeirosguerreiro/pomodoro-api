using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Domain.Achievements;

/// <summary>
/// Deriva <see cref="AchievementStats"/> a partir do histórico de sessões — pura, sem I/O.
/// "Dia" aqui é a data UTC da sessão: diferente do dia local usado em US-40 (progresso), uma
/// simplificação aceitável porque conquistas são marcos de gamificação, não números auditados.
/// </summary>
public static class AchievementCalculator
{
    public static AchievementStats BuildStats(
        IReadOnlyList<PomodoroSession> sessions, int completedTasksCount, int level)
    {
        var completedFocusCount = 0;
        var completedLongBreakCount = 0;
        var completedRestCount = 0;
        var focusCountByDay = new Dictionary<DateOnly, int>();

        foreach (var session in sessions)
        {
            if (session.Status != SessionStatus.Concluido)
            {
                continue;
            }

            if (session.Type == SessionType.Foco)
            {
                completedFocusCount++;
                var day = DateOnly.FromDateTime(session.CompletedAt);
                focusCountByDay[day] = focusCountByDay.GetValueOrDefault(day) + 1;
            }
            else
            {
                completedRestCount++;
                if (session.Type == SessionType.DescansoLongo)
                {
                    completedLongBreakCount++;
                }
            }
        }

        var maxFocusInOneDay = focusCountByDay.Count == 0 ? 0 : focusCountByDay.Values.Max();
        var longestStreakDays = LongestStreakDays(focusCountByDay.Keys.ToHashSet());

        return new AchievementStats(
            completedFocusCount, completedLongBreakCount, completedRestCount,
            maxFocusInOneDay, longestStreakDays, level, completedTasksCount);
    }

    /// <summary>Maior sequência de dias consecutivos com ao menos um foco concluído, em toda a história.</summary>
    private static int LongestStreakDays(IReadOnlySet<DateOnly> activeDates)
    {
        if (activeDates.Count == 0)
        {
            return 0;
        }

        var sortedDates = activeDates.OrderBy(d => d).ToList();
        var longest = 1;
        var current = 1;

        for (var i = 1; i < sortedDates.Count; i++)
        {
            if (sortedDates[i] == sortedDates[i - 1].AddDays(1))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }
}
