using FluentAssertions;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class SessionTypeDurationsTests
{
    [Theory]
    [InlineData(SessionType.Foco, SessionTypeDurations.FocoSeconds)]
    [InlineData(SessionType.DescansoCurto, SessionTypeDurations.DescansoCurtoSeconds)]
    [InlineData(SessionType.DescansoLongo, SessionTypeDurations.DescansoLongoSeconds)]
    public void StandardSecondsFor_RetornaDuracaoPadraoDoTipo(SessionType type, int expectedSeconds)
    {
        SessionTypeDurations.StandardSecondsFor(type).Should().Be(expectedSeconds);
    }

    [Fact]
    public void StandardSecondsFor_ComTipoDesconhecido_LancaArgumentOutOfRangeException()
    {
        var invalidType = (SessionType)999;

        var act = () => SessionTypeDurations.StandardSecondsFor(invalidType);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(SessionType.Foco, 5 * 60)]
    [InlineData(SessionType.DescansoCurto, 1 * 60)]
    [InlineData(SessionType.DescansoLongo, 5 * 60)]
    public void MinAllowedSecondsFor_RetornaPisoDaFaixaDoTipo(SessionType type, int expectedSeconds)
    {
        SessionTypeDurations.MinAllowedSecondsFor(type).Should().Be(expectedSeconds);
    }

    [Theory]
    [InlineData(SessionType.Foco, 120 * 60)]
    [InlineData(SessionType.DescansoCurto, 30 * 60)]
    [InlineData(SessionType.DescansoLongo, 60 * 60)]
    public void MaxAllowedSecondsFor_RetornaTetoDaFaixaDoTipo(SessionType type, int expectedSeconds)
    {
        SessionTypeDurations.MaxAllowedSecondsFor(type).Should().Be(expectedSeconds);
    }

    [Fact]
    public void MinAllowedSecondsFor_ComTipoDesconhecido_LancaArgumentOutOfRangeException()
    {
        var invalidType = (SessionType)999;

        var act = () => SessionTypeDurations.MinAllowedSecondsFor(invalidType);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MaxAllowedSecondsFor_ComTipoDesconhecido_LancaArgumentOutOfRangeException()
    {
        var invalidType = (SessionType)999;

        var act = () => SessionTypeDurations.MaxAllowedSecondsFor(invalidType);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
