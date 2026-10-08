using FluentAssertions;
using Pomodoro.Domain.Enums;
using Pomodoro.Domain.Services;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class ProgressRulesTests
{
    [Theory]
    [InlineData(SessionType.Foco, SessionStatus.Concluido, 25)]
    [InlineData(SessionType.DescansoCurto, SessionStatus.Concluido, 5)]
    [InlineData(SessionType.DescansoLongo, SessionStatus.Concluido, 5)]
    [InlineData(SessionType.Foco, SessionStatus.Interrompido, 0)]
    [InlineData(SessionType.DescansoCurto, SessionStatus.Interrompido, 0)]
    public void XpFor_RetornaXpConformeTipoEStatus(SessionType type, SessionStatus status, int expectedXp)
    {
        ProgressRules.XpFor(type, status).Should().Be(expectedXp);
    }

    [Theory]
    [InlineData(SessionType.Foco, SessionStatus.Concluido, 15)]
    [InlineData(SessionType.DescansoLongo, SessionStatus.Concluido, 10)]
    [InlineData(SessionType.DescansoCurto, SessionStatus.Concluido, 0)]
    [InlineData(SessionType.Foco, SessionStatus.Interrompido, 0)]
    public void SeedsFor_RetornaSementesConformeTipoEStatus(SessionType type, SessionStatus status, int expectedSeeds)
    {
        ProgressRules.SeedsFor(type, status).Should().Be(expectedSeeds);
    }

    [Theory]
    [InlineData(0, 1, 0, 200)]
    [InlineData(199, 1, 199, 200)]
    [InlineData(200, 2, 0, 400)]
    [InlineData(1020, 3, 420, 600)]
    public void CalculateLevel_CalculaNivelEProgressoCorretamente(int totalXp, int expectedLevel, int expectedXpInLevel, int expectedXpForNextLevel)
    {
        var (level, xpInLevel, xpForNextLevel) = ProgressRules.CalculateLevel(totalXp);

        level.Should().Be(expectedLevel);
        xpInLevel.Should().Be(expectedXpInLevel);
        xpForNextLevel.Should().Be(expectedXpForNextLevel);
    }

    [Theory]
    [InlineData(1, "Semente Curiosa")]
    [InlineData(2, "Broto Aprendiz")]
    [InlineData(3, "Jardineiro Produtivo")]
    [InlineData(4, "Jardineiro Produtivo")]
    [InlineData(5, "Horticultor Focado")]
    [InlineData(7, "Horticultor Focado")]
    [InlineData(8, "Mestre da Horta")]
    [InlineData(11, "Mestre da Horta")]
    [InlineData(12, "Guardião do Pomar")]
    [InlineData(100, "Guardião do Pomar")]
    public void TitleFor_RetornaTituloDoNivel(int level, string expectedTitle)
    {
        ProgressRules.TitleFor(level).Should().Be(expectedTitle);
    }

    [Fact]
    public void CalculateStreakDays_SemDiasAtivos_RetornaZero()
    {
        var today = new DateOnly(2026, 1, 10);

        ProgressRules.CalculateStreakDays(new HashSet<DateOnly>(), today).Should().Be(0);
    }

    [Fact]
    public void CalculateStreakDays_ComFocoHoje_ContaAPartirDeHoje()
    {
        var today = new DateOnly(2026, 1, 10);
        var activeDates = new HashSet<DateOnly> { today, today.AddDays(-1), today.AddDays(-2) };

        ProgressRules.CalculateStreakDays(activeDates, today).Should().Be(3);
    }

    [Fact]
    public void CalculateStreakDays_SemFocoHojeMasComOntem_StreakContinuaViva()
    {
        var today = new DateOnly(2026, 1, 10);
        var activeDates = new HashSet<DateOnly> { today.AddDays(-1), today.AddDays(-2) };

        ProgressRules.CalculateStreakDays(activeDates, today).Should().Be(2);
    }

    [Fact]
    public void CalculateStreakDays_SemFocoHojeNemOntem_RetornaZero()
    {
        var today = new DateOnly(2026, 1, 10);
        var activeDates = new HashSet<DateOnly> { today.AddDays(-2) };

        ProgressRules.CalculateStreakDays(activeDates, today).Should().Be(0);
    }

    [Fact]
    public void CalculateStreakDays_ParaNoPrimeiroDiaSemFoco()
    {
        var today = new DateOnly(2026, 1, 10);
        var activeDates = new HashSet<DateOnly> { today, today.AddDays(-1), today.AddDays(-3) };

        ProgressRules.CalculateStreakDays(activeDates, today).Should().Be(2);
    }
}
