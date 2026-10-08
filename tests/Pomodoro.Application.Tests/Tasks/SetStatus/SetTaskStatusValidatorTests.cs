using FluentAssertions;
using Pomodoro.Application.Tasks.SetStatus;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.SetStatus;

public class SetTaskStatusValidatorTests
{
    private readonly SetTaskStatusValidator _validator = new();

    [Theory]
    [InlineData("a_fazer")]
    [InlineData("em_curso")]
    [InlineData("feito")]
    public void Validate_ComStatusValido_NaoRetornaErros(string status)
    {
        _validator.Validate(new SetTaskStatusRequest(status)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ComStatusInvalido_RetornaErroNoCampoStatus()
    {
        var result = _validator.Validate(new SetTaskStatusRequest("pausado"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }
}
