using FluentAssertions;
using Pomodoro.Application.Users.ChangePassword;
using Xunit;

namespace Pomodoro.Application.Tests.Users.ChangePassword;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        _validator.Validate(new ChangePasswordRequest("SenhaAtual1", "SenhaNova123")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ComSenhaAtualVazia_RetornaErroNoCampoCurrentPassword(string currentPassword)
    {
        var result = _validator.Validate(new ChangePasswordRequest(currentPassword, "SenhaNova123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Validate_ComNovaSenhaForaDaRegra_RetornaErroNoCampoNewPassword(string newPassword)
    {
        var result = _validator.Validate(new ChangePasswordRequest("SenhaAtual1", newPassword));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
