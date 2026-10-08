using FluentAssertions;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Common;

public class SessionWireFormatTests
{
    [Theory]
    [InlineData("foco", SessionType.Foco)]
    [InlineData("descanso_curto", SessionType.DescansoCurto)]
    [InlineData("descanso_longo", SessionType.DescansoLongo)]
    public void TryParseType_ComValorValido_RetornaTrueETipoCorreto(string wire, SessionType expected)
    {
        var ok = SessionWireFormat.TryParseType(wire, out var type);

        ok.Should().BeTrue();
        type.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("almoco")]
    public void TryParseType_ComValorInvalido_RetornaFalse(string? wire)
    {
        var ok = SessionWireFormat.TryParseType(wire, out _);

        ok.Should().BeFalse();
    }

    [Theory]
    [InlineData(SessionType.Foco, "foco")]
    [InlineData(SessionType.DescansoCurto, "descanso_curto")]
    [InlineData(SessionType.DescansoLongo, "descanso_longo")]
    public void ToWire_Type_RetornaLiteralCorreto(SessionType type, string expected)
    {
        type.ToWire().Should().Be(expected);
    }

    [Fact]
    public void ToWire_Type_ComValorDesconhecido_LancaArgumentOutOfRangeException()
    {
        var act = () => ((SessionType)999).ToWire();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("concluido", SessionStatus.Concluido)]
    [InlineData("interrompido", SessionStatus.Interrompido)]
    public void TryParseStatus_ComValorValido_RetornaTrueEStatusCorreto(string wire, SessionStatus expected)
    {
        var ok = SessionWireFormat.TryParseStatus(wire, out var status);

        ok.Should().BeTrue();
        status.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pausado")]
    public void TryParseStatus_ComValorInvalido_RetornaFalse(string? wire)
    {
        var ok = SessionWireFormat.TryParseStatus(wire, out _);

        ok.Should().BeFalse();
    }

    [Theory]
    [InlineData(SessionStatus.Concluido, "concluido")]
    [InlineData(SessionStatus.Interrompido, "interrompido")]
    public void ToWire_Status_RetornaLiteralCorreto(SessionStatus status, string expected)
    {
        status.ToWire().Should().Be(expected);
    }

    [Fact]
    public void ToWire_Status_ComValorDesconhecido_LancaArgumentOutOfRangeException()
    {
        var act = () => ((SessionStatus)999).ToWire();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
