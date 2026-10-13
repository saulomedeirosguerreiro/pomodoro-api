using FluentAssertions;
using Pomodoro.Application.Users.UpdateName;
using Xunit;

namespace Pomodoro.Application.Tests.Users.UpdateName;

public class UpdateNameValidatorTests
{
    private readonly UpdateNameValidator _validator = new();

    [Fact]
    public void Validate_ComNomeValido_NaoRetornaErros()
    {
        _validator.Validate(new UpdateNameRequest("João Silva")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Validate_ComNomeInvalido_RetornaErroNoCampoName(string name)
    {
        var result = _validator.Validate(new UpdateNameRequest(name));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ComNomeMaiorQue100Caracteres_RetornaErroNoCampoName()
    {
        var longName = new string('a', 101);

        var result = _validator.Validate(new UpdateNameRequest(longName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
