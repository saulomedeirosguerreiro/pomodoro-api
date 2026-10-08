using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Services;

namespace Pomodoro.Domain.Achievements;

/// <summary>
/// Localiza o instante histórico em que uma conquista monotônica (todas as de <see cref="AchievementCatalog"/>)
/// passou a valer, por busca binária sobre o histórico ordenado — usado só na importação em lote, onde a
/// avaliação roda depois que dezenas/centenas de eventos já aconteceram de uma vez (ao contrário do uso
/// normal, onde <c>EvaluateAchievementsHandler</c> roda logo após cada evento e <c>UtcNow</c> já é uma boa
/// aproximação). Todas as 10 definições do catálogo são predicados "contador ≥ N" sobre
/// <see cref="AchievementStats"/>, logo são monotônicas sobre um prefixo cronológico do histórico — o que
/// permite O(N log N) em vez de um replay O(N²).
/// </summary>
public static class AchievementTimelineResolver
{
    /// <returns>
    /// O timestamp do evento mais antigo cujo prefixo cronológico já satisfaz <paramref name="isUnlocked"/>,
    /// ou <c>null</c> se nem o histórico completo satisfaz (não deveria acontecer quando o chamador só invoca
    /// esta função para conquistas já confirmadas como destravadas pelos stats finais — tratado aqui como
    /// fallback defensivo, nunca como caminho esperado).
    /// </returns>
    public static DateTime? FindUnlockMoment(
        IReadOnlyList<PomodoroSession> allSessionsChronological,
        IReadOnlyList<DateTime> taskCompletionTimestampsChronological,
        int finalCompletedTasksCount,
        Func<AchievementStats, bool> isUnlocked)
    {
        var timeline = BuildTimeline(allSessionsChronological, taskCompletionTimestampsChronological);
        if (timeline.Count == 0)
        {
            return null;
        }

        var lastIndex = timeline.Count - 1;
        if (!isUnlocked(BuildStatsAt(timeline, lastIndex, allSessionsChronological, finalCompletedTasksCount)))
        {
            return null;
        }

        var low = 0;
        var high = lastIndex;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);

            // `mid` nunca é igual a `lastIndex` aqui (invariante de `while (low < high)`: mid fica sempre
            // em [low, high-1], e high começa em lastIndex e só diminui) — a contagem corrida natural da
            // timeline é sempre a correta para qualquer índice visitado pela busca; o valor autoritativo
            // `finalCompletedTasksCount` só precisa substituir a contagem no ponto final, já tratado
            // separadamente pela checagem de `lastIndex` antes do laço começar.
            var stats = BuildStatsAt(timeline, mid, allSessionsChronological, timeline[mid].TaskCountSoFar);

            if (isUnlocked(stats))
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return timeline[low].Timestamp;
    }

    /// <summary>Quantas sessões/tarefas concluídas já tinham acontecido na linha do tempo mesclada até este evento.</summary>
    private readonly record struct TimelineEvent(DateTime Timestamp, int SessionCountSoFar, int TaskCountSoFar);

    /// <summary>Mescla sessões e conclusões de tarefa (ambos já ordenados individualmente) por timestamp.</summary>
    private static List<TimelineEvent> BuildTimeline(
        IReadOnlyList<PomodoroSession> sessions, IReadOnlyList<DateTime> taskTimestamps)
    {
        var timeline = new List<TimelineEvent>(sessions.Count + taskTimestamps.Count);
        var sessionIndex = 0;
        var taskIndex = 0;
        var sessionsSoFar = 0;
        var tasksSoFar = 0;

        while (sessionIndex < sessions.Count || taskIndex < taskTimestamps.Count)
        {
            var takeSession = taskIndex >= taskTimestamps.Count
                || (sessionIndex < sessions.Count && sessions[sessionIndex].CompletedAt <= taskTimestamps[taskIndex]);

            DateTime timestamp;
            if (takeSession)
            {
                timestamp = sessions[sessionIndex].CompletedAt;
                sessionsSoFar++;
                sessionIndex++;
            }
            else
            {
                timestamp = taskTimestamps[taskIndex];
                tasksSoFar++;
                taskIndex++;
            }

            timeline.Add(new TimelineEvent(timestamp, sessionsSoFar, tasksSoFar));
        }

        return timeline;
    }

    private static AchievementStats BuildStatsAt(
        IReadOnlyList<TimelineEvent> timeline, int index,
        IReadOnlyList<PomodoroSession> allSessionsChronological, int taskCount)
    {
        var sessionCount = timeline[index].SessionCountSoFar;
        var prefixSessions = allSessionsChronological.Take(sessionCount).ToList();
        var totalXp = prefixSessions.Sum(s => ProgressRules.XpFor(s.Type, s.Status));
        var (level, _, _) = ProgressRules.CalculateLevel(totalXp);

        return AchievementCalculator.BuildStats(prefixSessions, taskCount, level);
    }
}
