using FluentAssertions;
using Pomodoro.Domain.Common;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class UserTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ComDadosValidos_PreencheTodosOsCampos()
    {
        var user = User.Create(" João ", " joao@email.com ", "hash123", UtcNow);

        user.Name.Should().Be("João");
        user.Email.Should().Be("joao@email.com");
        user.PasswordHash.Should().Be("hash123");
        user.CreatedAt.Should().Be(UtcNow);
        user.UpdatedAt.Should().Be(UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComNomeInvalido_LancaDomainException(string? name)
    {
        var act = () => User.Create(name!, "joao@email.com", "hash123", UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComEmailInvalido_LancaDomainException(string? email)
    {
        var act = () => User.Create("João", email!, "hash123", UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComHashDeSenhaInvalido_LancaDomainException(string? passwordHash)
    {
        var act = () => User.Create("João", "joao@email.com", passwordHash!, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdatePassword_ComHashValido_AtualizaHashEUpdatedAt()
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", UtcNow);
        var later = UtcNow.AddDays(1);

        user.UpdatePassword("hash-novo", later);

        user.PasswordHash.Should().Be("hash-novo");
        user.UpdatedAt.Should().Be(later);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdatePassword_ComHashVazioOuEmBranco_LancaDomainException(string? newPasswordHash)
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", UtcNow);

        var act = () => user.UpdatePassword(newPasswordHash!, UtcNow.AddDays(1));

        act.Should().Throw<DomainException>();
    }
}
