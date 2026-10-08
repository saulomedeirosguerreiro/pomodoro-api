using FluentAssertions;
using Pomodoro.Application.Auth.RecoverPassword;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.RecoverPassword;

public class RecoverPasswordValidatorTests
{
    private readonly RecoverPasswordValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(new RecoverPasswordRequest("João", "joao@email.com", "Senha123"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ComNomeVazio_RetornaErroNoCampoName(string name)
    {
        var result = _validator.Validate(new RecoverPasswordRequest(name, "joao@email.com", "Senha123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    public void Validate_ComEmailInvalido_RetornaErroNoCampoEmail(string email)
    {
        var result = _validator.Validate(new RecoverPasswordRequest("João", email, "Senha123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Validate_ComSenhaForaDaRegra_RetornaErroNoCampoNewPassword(string newPassword)
    {
        var result = _validator.Validate(new RecoverPasswordRequest("João", "joao@email.com", newPassword));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Validate_ComSenhaMaiorQue72Caracteres_RetornaErroNoCampoNewPassword()
    {
        var longPassword = new string('a', 71) + "1A";
        var result = _validator.Validate(new RecoverPasswordRequest("João", "joao@email.com", longPassword));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
    }
}
