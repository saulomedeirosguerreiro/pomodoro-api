using FluentAssertions;
using Pomodoro.Domain.Achievements;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Pomodoro.Domain.Services;
using Xunit;

namespace Pomodoro.Domain.Tests.Achievements;

public class AchievementTimelineResolverTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static PomodoroSession BuildFocusSession(DateTime completedAt) =>
        PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
            completedAt.AddSeconds(-SessionTypeDurations.FocoSeconds), completedAt, completedAt);

    private static AchievementDefinition Find(string code) => AchievementCatalog.All.Single(a => a.Code == code);

    [Fact]
    public void FindUnlockMoment_HistoricoDeSeteDiasSeguidosDeFoco_Constancia7ResolveNoUltimoFocoDoSetimoDia()
    {
        var sessions = Enumerable.Range(0, 7)
            .Select(i => BuildFocusSession(BaseTime.AddDays(i)))
            .ToList();

        var result = AchievementTimelineResolver.FindUnlockMoment(
            sessions, Array.Empty<DateTime>(), finalCompletedTasksCount: 0, Find("constancia_7").IsUnlocked);

        result.Should().Be(sessions[6].CompletedAt);
    }

    [Fact]
    public void FindUnlockMoment_DataResolvida_NuncaEMaiorQueOUltimoEventoDoHistorico()
    {
        var sessions = Enumerable.Range(0, 10)
            .Select(i => BuildFocusSession(BaseTime.AddDays(i)))
            .ToList();

        var result = AchievementTimelineResolver.FindUnlockMoment(
            sessions, Array.Empty<DateTime>(), finalCompletedTasksCount: 0, Find("constancia_3").IsUnlocked);

        result.Should().NotBeNull();
        result!.Value.Should().BeOnOrBefore(sessions[^1].CompletedAt);
    }

    [Fact]
    public void FindUnlockMoment_ConjuntoDeCodigosDestravaveis_BateComAchievementCatalogAvaliadoNosStatsFinais()
    {
        var sessions = Enumerable.Range(0, 10)
            .Select(i => BuildFocusSession(BaseTime.AddDays(i)))
            .ToList();
        var taskTimestamps = new[] { BaseTime.AddDays(5) };

        var totalXp = sessions.Sum(s => ProgressRules.XpFor(s.Type, s.Status));
        var (level, _, _) = ProgressRules.CalculateLevel(totalXp);
        var finalStats = AchievementCalculator.BuildStats(sessions, completedTasksCount: 1, level);

        foreach (var definition in AchievementCatalog.All)
        {
            var expectedUnlocked = definition.IsUnlocked(finalStats);

            var result = AchievementTimelineResolver.FindUnlockMoment(
                sessions, taskTimestamps, finalCompletedTasksCount: 1, definition.IsUnlocked);

            (result is not null).Should().Be(expectedUnlocked, because: $"código {definition.Code}");
        }
    }

    [Fact]
    public void FindUnlockMoment_ComPredicadoQueNuncaFicaVerdadeiro_RetornaNull()
    {
        var sessions = new List<PomodoroSession> { BuildFocusSession(BaseTime) };

        var result = AchievementTimelineResolver.FindUnlockMoment(
            sessions, Array.Empty<DateTime>(), finalCompletedTasksCount: 0, _ => false);

        result.Should().BeNull();
    }

    [Fact]
    public void FindUnlockMoment_ComHistoricoVazio_RetornaNull()
    {
        var result = AchievementTimelineResolver.FindUnlockMoment(
            Array.Empty<PomodoroSession>(), Array.Empty<DateTime>(), finalCompletedTasksCount: 0, _ => true);

        result.Should().BeNull();
    }

    [Fact]
    public void FindUnlockMoment_ComSessoesETarefasIntercaladas_MesclaATimelinePorOrdemCronologica()
    {
        var sessions = new List<PomodoroSession>
        {
            BuildFocusSession(BaseTime),
            BuildFocusSession(BaseTime.AddDays(2)),
            BuildFocusSession(BaseTime.AddDays(4)),
        };
        var taskTimestamps = new[] { BaseTime.AddDays(1), BaseTime.AddDays(3), BaseTime.AddDays(5) };

        var result = AchievementTimelineResolver.FindUnlockMoment(
            sessions, taskTimestamps, finalCompletedTasksCount: 3, Find("primeira_colheita").IsUnlocked);

        result.Should().Be(taskTimestamps[0]);
    }

    [Fact]
    public void FindUnlockMoment_ApenasComTimestampsDeTarefaSemSessoes_ResolveSemConsultarSessoes()
    {
        var taskTimestamps = new[] { BaseTime, BaseTime.AddDays(1) };

        var result = AchievementTimelineResolver.FindUnlockMoment(
            Array.Empty<PomodoroSession>(), taskTimestamps, finalCompletedTasksCount: 2,
            Find("primeira_colheita").IsUnlocked);

        result.Should().Be(taskTimestamps[0]);
    }

    [Fact]
    public void FindUnlockMoment_Nivel5_ResolveExatamenteNoEventoQueAtingeOXpNecessario_NaoAntes()
    {
        // FocoXp = 25 por sessão; nível 5 exige 2000 XP acumulado => exatamente a 80ª sessão de foco.
        const int sessionsNeededForLevel5 = 80;
        var sessions = Enumerable.Range(0, sessionsNeededForLevel5 + 5)
            .Select(i => BuildFocusSession(BaseTime.AddHours(i)))
            .ToList();

        var result = AchievementTimelineResolver.FindUnlockMoment(
            sessions, Array.Empty<DateTime>(), finalCompletedTasksCount: 0, Find("nivel_5").IsUnlocked);

        result.Should().Be(sessions[sessionsNeededForLevel5 - 1].CompletedAt);
    }
}
