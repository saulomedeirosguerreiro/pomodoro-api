using FluentAssertions;
using Pomodoro.Application.Users.Delete;
using Xunit;

namespace Pomodoro.Application.Tests.Users.Delete;

public class DeleteAccountValidatorTests
{
    private readonly DeleteAccountValidator _validator = new();

    [Fact]
    public void Validate_ComSenhaPreenchida_NaoRetornaErros()
    {
        _validator.Validate(new DeleteAccountRequest("Senha123")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ComSenhaVazia_RetornaErroNoCampoPassword(string password)
    {
        var result = _validator.Validate(new DeleteAccountRequest(password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
