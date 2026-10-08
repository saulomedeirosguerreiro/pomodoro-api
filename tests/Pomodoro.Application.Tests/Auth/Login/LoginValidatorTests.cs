using FluentAssertions;
using Pomodoro.Application.Auth.Login;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.Login;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoRetornaErros()
    {
        _validator.Validate(new LoginRequest("joao@email.com", "Senha123")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_SemEmail_RetornaErro()
    {
        _validator.Validate(new LoginRequest("", "Senha123")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SemSenha_RetornaErro()
    {
        _validator.Validate(new LoginRequest("joao@email.com", "")).IsValid.Should().BeFalse();
    }
}
