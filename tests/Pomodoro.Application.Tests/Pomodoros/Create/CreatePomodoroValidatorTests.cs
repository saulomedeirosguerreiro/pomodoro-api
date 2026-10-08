using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Common;
using Pomodoro.Application.Pomodoros.Create;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Pomodoros.Create;

public class CreatePomodoroValidatorTests
{
    private static readonly DateTime StartedAt = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CompletedAt = StartedAt.AddSeconds(SessionTypeDurations.FocoSeconds);

    // "Agora" bem depois das datas fixas de teste, para não disparar a regra de data futura por acidente.
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly CreatePomodoroValidator _validator;

    public CreatePomodoroValidatorTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        _validator = new CreatePomodoroValidator(_clock);
    }

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt);

        _validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ComTipoInvalido_RetornaErroNoCampoType()
    {
        var request = new CreatePomodoroRequest("almoco", "concluido", 60, StartedAt, CompletedAt);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void Validate_ComStatusInvalido_RetornaErroNoCampoStatus()
    {
        var request = new CreatePomodoroRequest("foco", "pausado", 60, StartedAt, CompletedAt);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_ComDuracaoNaoPositiva_RetornaErroNoCampoDurationSeconds(int duration)
    {
        var request = new CreatePomodoroRequest("foco", "interrompido", duration, StartedAt, CompletedAt);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationSeconds");
    }

    [Fact]
    public void Validate_ComDuracaoAcimaDoMaximoDoTipo_RetornaErroNoCampoDurationSeconds()
    {
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.MaxAllowedSecondsFor(SessionType.Foco) + 1, StartedAt, CompletedAt);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DurationSeconds");
    }

    [Fact]
    public void Validate_ComCompletedAtAnteriorAoStartedAt_RetornaErroNoCampoCompletedAt()
    {
        var request = new CreatePomodoroRequest(
            "foco", "interrompido", 60, StartedAt, StartedAt.AddSeconds(-1));

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompletedAt");
    }

    [Fact]
    public void Validate_ComStartedAtNoFuturo_RetornaErroNoCampoStartedAt()
    {
        var now = _clock.UtcNow;
        var futureStart = now.AddHours(1);
        var request = new CreatePomodoroRequest("foco", "interrompido", 60, futureStart, futureStart.AddSeconds(60));

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartedAt");
    }

    [Fact]
    public void Validate_ComCompletedAtNoFuturo_RetornaErroNoCampoCompletedAt()
    {
        var now = _clock.UtcNow;
        var request = new CreatePomodoroRequest("foco", "concluido", 60, now.AddSeconds(-60), now.AddHours(1));

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompletedAt");
    }

    [Fact]
    public void Validate_ComCompletedAtDentroDaToleranciaDeRelogio_NaoRetornaErro()
    {
        var now = _clock.UtcNow;
        var request = new CreatePomodoroRequest("foco", "concluido", 60, now.AddSeconds(-60), now.AddSeconds(30));

        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
