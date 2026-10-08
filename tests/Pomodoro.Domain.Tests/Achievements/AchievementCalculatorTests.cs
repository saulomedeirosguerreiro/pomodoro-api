using FluentAssertions;
using Pomodoro.Domain.Achievements;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Domain.Tests.Achievements;

public class AchievementCalculatorTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static PomodoroSession BuildSession(SessionType type, SessionStatus status, DateTime completedAt) =>
        PomodoroSession.Create(
            1, type, status, SessionTypeDurations.StandardSecondsFor(type),
            completedAt.AddSeconds(-SessionTypeDurations.StandardSecondsFor(type)), completedAt, completedAt);

    [Fact]
    public void BuildStats_SemSessoes_RetornaTudoZerado()
    {
        var stats = AchievementCalculator.BuildStats(new List<PomodoroSession>(), completedTasksCount: 0, level: 1);

        stats.CompletedFocusCount.Should().Be(0);
        stats.CompletedLongBreakCount.Should().Be(0);
        stats.CompletedRestCount.Should().Be(0);
        stats.MaxFocusInOneDay.Should().Be(0);
        stats.LongestStreakDays.Should().Be(0);
        stats.Level.Should().Be(1);
        stats.CompletedTasksCount.Should().Be(0);
    }

    [Fact]
    public void BuildStats_ContaApenasSessoesConcluidas()
    {
        var sessions = new List<PomodoroSession>
        {
            BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime),
            BuildSession(SessionType.Foco, SessionStatus.Interrompido, BaseTime.AddMinutes(30)),
            BuildSession(SessionType.DescansoCurto, SessionStatus.Concluido, BaseTime.AddMinutes(31)),
            BuildSession(SessionType.DescansoLongo, SessionStatus.Concluido, BaseTime.AddMinutes(32)),
        };

        var stats = AchievementCalculator.BuildStats(sessions, completedTasksCount: 2, level: 3);

        stats.CompletedFocusCount.Should().Be(1);
        stats.CompletedRestCount.Should().Be(2);
        stats.CompletedLongBreakCount.Should().Be(1);
        stats.CompletedTasksCount.Should().Be(2);
        stats.Level.Should().Be(3);
    }

    [Fact]
    public void BuildStats_MaxFocusInOneDay_ContaOMaiorNumeroDeFocosNoMesmoDiaUtc()
    {
        var day1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        var sessions = new List<PomodoroSession>
        {
            BuildSession(SessionType.Foco, SessionStatus.Concluido, day1),
            BuildSession(SessionType.Foco, SessionStatus.Concluido, day1.AddMinutes(30)),
            BuildSession(SessionType.Foco, SessionStatus.Concluido, day1.AddMinutes(60)),
            BuildSession(SessionType.Foco, SessionStatus.Concluido, day2),
        };

        var stats = AchievementCalculator.BuildStats(sessions, completedTasksCount: 0, level: 1);

        stats.MaxFocusInOneDay.Should().Be(3);
    }

    [Fact]
    public void BuildStats_LongestStreakDays_ComDiasConsecutivos_RetornaOTamanhoDaSequencia()
    {
        var sessions = Enumerable.Range(0, 5)
            .Select(i => BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(i)))
            .ToList();

        var stats = AchievementCalculator.BuildStats(sessions, completedTasksCount: 0, level: 1);

        stats.LongestStreakDays.Should().Be(5);
    }

    [Fact]
    public void BuildStats_LongestStreakDays_ComLacuna_RetornaAMaiorSequenciaIsolada()
    {
        var sessions = new List<PomodoroSession>
        {
            BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime),
            BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(1)),
            BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(2)),
            // lacuna no dia 3
            BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(4)),
        };

        var stats = AchievementCalculator.BuildStats(sessions, completedTasksCount: 0, level: 1);

        stats.LongestStreakDays.Should().Be(3);
    }

    [Fact]
    public void BuildStats_LongestStreakDays_RetornaOMaiorMesmoQuandoAStreakAtualFoiQuebrada()
    {
        // Sequência antiga de 7 dias quebrada, seguida de uma sequência atual de 2 dias —
        // a conquista de constância deve considerar o recorde histórico, não só o streak vigente.
        var sessions = Enumerable.Range(0, 7)
            .Select(i => BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(i)))
            .Concat(new[]
            {
                BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(20)),
                BuildSession(SessionType.Foco, SessionStatus.Concluido, BaseTime.AddDays(21)),
            })
            .ToList();

        var stats = AchievementCalculator.BuildStats(sessions, completedTasksCount: 0, level: 1);

        stats.LongestStreakDays.Should().Be(7);
    }
}
