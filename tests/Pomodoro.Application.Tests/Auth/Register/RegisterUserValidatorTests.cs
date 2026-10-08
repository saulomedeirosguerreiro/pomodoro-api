using FluentAssertions;
using Pomodoro.Application.Auth.Register;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.Register;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(new RegisterUserRequest("João", "joao@email.com", "Senha123"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Validate_ComNomeInvalido_RetornaErroNoCampoName(string name)
    {
        var result = _validator.Validate(new RegisterUserRequest(name, "joao@email.com", "Senha123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    public void Validate_ComEmailInvalido_RetornaErroNoCampoEmail(string email)
    {
        var result = _validator.Validate(new RegisterUserRequest("João", email, "Senha123"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("somenteletras")]
    [InlineData("12345678")]
    public void Validate_ComSenhaForaDaRegra_RetornaErroNoCampoPassword(string password)
    {
        var result = _validator.Validate(new RegisterUserRequest("João", "joao@email.com", password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_ComSenhaMaiorQue72Caracteres_RetornaErroNoCampoPassword()
    {
        var longPassword = new string('a', 71) + "1A";
        var result = _validator.Validate(new RegisterUserRequest("João", "joao@email.com", longPassword));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
