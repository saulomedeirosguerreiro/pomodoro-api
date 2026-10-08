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

    [Fact]
    public void MaxAllowedSecondsFor_SomaTolerancia()
    {
        SessionTypeDurations.MaxAllowedSecondsFor(SessionType.Foco)
            .Should().Be(SessionTypeDurations.FocoSeconds + SessionTypeDurations.ToleranceSeconds);
    }
}
