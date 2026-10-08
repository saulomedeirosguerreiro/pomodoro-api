using FluentAssertions;
using Pomodoro.Domain.Enums;
using Pomodoro.Domain.Services;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class PomodoroCycleAdvisorTests
{
    [Theory]
    [InlineData(0, SessionType.DescansoCurto)]
    [InlineData(1, SessionType.DescansoCurto)]
    [InlineData(2, SessionType.DescansoCurto)]
    [InlineData(3, SessionType.DescansoCurto)]
    [InlineData(4, SessionType.DescansoLongo)]
    [InlineData(8, SessionType.DescansoLongo)]
    public void NextSuggestedType_AposFoco_SugereCurtoOuLongoConformeCiclo(int totalFociCompletedSoFar, SessionType expected)
    {
        var next = PomodoroCycleAdvisor.NextSuggestedType(SessionType.Foco, totalFociCompletedSoFar);

        next.Should().Be(expected);
    }

    [Theory]
    [InlineData(SessionType.DescansoCurto)]
    [InlineData(SessionType.DescansoLongo)]
    public void NextSuggestedType_AposDescanso_SugereFoco(SessionType completedType)
    {
        var next = PomodoroCycleAdvisor.NextSuggestedType(completedType, totalFociCompletedSoFar: 0);

        next.Should().Be(SessionType.Foco);
    }
}
