using FluentAssertions;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Common;

public class TaskWireFormatTests
{
    [Theory]
    [InlineData("baixa", TaskPriority.Baixa)]
    [InlineData("media", TaskPriority.Media)]
    [InlineData("alta", TaskPriority.Alta)]
    public void TryParsePriority_ComValorValido_RetornaTrueEPrioridadeCorreta(string wire, TaskPriority expected)
    {
        var ok = TaskWireFormat.TryParsePriority(wire, out var priority);

        ok.Should().BeTrue();
        priority.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("urgente")]
    public void TryParsePriority_ComValorInvalido_RetornaFalse(string? wire)
    {
        TaskWireFormat.TryParsePriority(wire, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskPriority.Baixa, "baixa")]
    [InlineData(TaskPriority.Media, "media")]
    [InlineData(TaskPriority.Alta, "alta")]
    public void ToWire_Priority_RetornaLiteralCorreto(TaskPriority priority, string expected)
    {
        priority.ToWire().Should().Be(expected);
    }

    [Fact]
    public void ToWire_Priority_ComValorDesconhecido_LancaArgumentOutOfRangeException()
    {
        var act = () => ((TaskPriority)999).ToWire();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("a_fazer", TaskItemStatus.AFazer)]
    [InlineData("em_curso", TaskItemStatus.EmCurso)]
    [InlineData("feito", TaskItemStatus.Feito)]
    public void TryParseStatus_ComValorValido_RetornaTrueEStatusCorreto(string wire, TaskItemStatus expected)
    {
        var ok = TaskWireFormat.TryParseStatus(wire, out var status);

        ok.Should().BeTrue();
        status.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pausado")]
    public void TryParseStatus_ComValorInvalido_RetornaFalse(string? wire)
    {
        TaskWireFormat.TryParseStatus(wire, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskItemStatus.AFazer, "a_fazer")]
    [InlineData(TaskItemStatus.EmCurso, "em_curso")]
    [InlineData(TaskItemStatus.Feito, "feito")]
    public void ToWire_Status_RetornaLiteralCorreto(TaskItemStatus status, string expected)
    {
        status.ToWire().Should().Be(expected);
    }

    [Fact]
    public void ToWire_Status_ComValorDesconhecido_LancaArgumentOutOfRangeException()
    {
        var act = () => ((TaskItemStatus)999).ToWire();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
