using FluentAssertions;
using Pomodoro.Domain.Achievements;
using Xunit;

namespace Pomodoro.Domain.Tests.Achievements;

public class AchievementCatalogTests
{
    private static AchievementStats Stats(
        int completedFocusCount = 0,
        int completedLongBreakCount = 0,
        int completedRestCount = 0,
        int maxFocusInOneDay = 0,
        int longestStreakDays = 0,
        int level = 1,
        int completedTasksCount = 0) => new(
        completedFocusCount, completedLongBreakCount, completedRestCount,
        maxFocusInOneDay, longestStreakDays, level, completedTasksCount);

    private static AchievementDefinition Find(string code) =>
        AchievementCatalog.All.Single(a => a.Code == code);

    [Fact]
    public void Catalogo_TemDezConquistasComCodigosUnicos()
    {
        AchievementCatalog.All.Should().HaveCount(10);
        AchievementCatalog.All.Select(a => a.Code).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData("primeira_semente")]
    [InlineData("ciclo_completo")]
    [InlineData("dia_fertil")]
    [InlineData("constancia_3")]
    [InlineData("constancia_7")]
    [InlineData("constancia_30")]
    [InlineData("primeira_colheita")]
    [InlineData("cem_tomates")]
    [InlineData("nivel_5")]
    [InlineData("descanso_sabio")]
    public void Catalogo_ContemCadaCodigoEsperado(string code)
    {
        AchievementCatalog.All.Should().Contain(a => a.Code == code);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void PrimeiraSemente_DesbloqueiaComUmFocoConcluido(int completedFocusCount, bool expected)
    {
        Find("primeira_semente").IsUnlocked(Stats(completedFocusCount: completedFocusCount)).Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 1, false)]
    [InlineData(4, 0, false)]
    [InlineData(4, 1, true)]
    [InlineData(5, 2, true)]
    public void CicloCompleto_ExigeQuatroFocosEUmaPausaLonga(int focusCount, int longBreakCount, bool expected)
    {
        Find("ciclo_completo").IsUnlocked(Stats(completedFocusCount: focusCount, completedLongBreakCount: longBreakCount))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    public void DiaFertil_ExigeOitoFocosNoMesmoDia(int maxFocusInOneDay, bool expected)
    {
        Find("dia_fertil").IsUnlocked(Stats(maxFocusInOneDay: maxFocusInOneDay)).Should().Be(expected);
    }

    [Fact]
    public void DiaFertil_Progresso_NaoUltrapassaOAlvo()
    {
        var progress = Find("dia_fertil").Progress(Stats(maxFocusInOneDay: 20));

        progress!.Current.Should().Be(8);
        progress.Target.Should().Be(8);
    }

    [Theory]
    [InlineData("constancia_3", 2, false)]
    [InlineData("constancia_3", 3, true)]
    [InlineData("constancia_7", 6, false)]
    [InlineData("constancia_7", 7, true)]
    [InlineData("constancia_30", 29, false)]
    [InlineData("constancia_30", 30, true)]
    public void Constancia_ExigeOStreakMinimoCorrespondente(string code, int longestStreakDays, bool expected)
    {
        Find(code).IsUnlocked(Stats(longestStreakDays: longestStreakDays)).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void PrimeiraColheita_DesbloqueiaComUmaTarefaConcluida(int completedTasksCount, bool expected)
    {
        Find("primeira_colheita").IsUnlocked(Stats(completedTasksCount: completedTasksCount)).Should().Be(expected);
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(120, true)]
    public void CemTomates_ExigeCemFocosConcluidos(int completedFocusCount, bool expected)
    {
        Find("cem_tomates").IsUnlocked(Stats(completedFocusCount: completedFocusCount)).Should().Be(expected);
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public void Nivel5_ExigeNivelMinimoCinco(int level, bool expected)
    {
        Find("nivel_5").IsUnlocked(Stats(level: level)).Should().Be(expected);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public void DescansoSabio_ExigeDezPausasConcluidas(int completedRestCount, bool expected)
    {
        Find("descanso_sabio").IsUnlocked(Stats(completedRestCount: completedRestCount)).Should().Be(expected);
    }

    [Theory]
    [InlineData("primeira_semente")]
    [InlineData("ciclo_completo")]
    [InlineData("primeira_colheita")]
    public void ConquistasBinarias_NaoTemProgressoNumerico(string code)
    {
        Find(code).Progress(Stats()).Should().BeNull();
    }
}
