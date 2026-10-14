using FluentAssertions;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class RefreshTokenTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_DefineCamposEFicaAtivo()
    {
        var expiresAt = UtcNow.AddDays(30);

        var token = RefreshToken.Create(1, "hash", UtcNow, expiresAt);

        token.UserId.Should().Be(1);
        token.TokenHash.Should().Be("hash");
        token.ExpiresAt.Should().Be(expiresAt);
        token.RevokedAt.Should().BeNull();
        token.IsActive(UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsActive_ComTokenExpirado_RetornaFalse()
    {
        var token = RefreshToken.Create(1, "hash", UtcNow, UtcNow.AddDays(30));

        token.IsActive(UtcNow.AddDays(31)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_MarcaRevokedAtENaoFicaMaisAtivo()
    {
        var token = RefreshToken.Create(1, "hash", UtcNow, UtcNow.AddDays(30));

        token.Revoke(UtcNow.AddDays(1), replacedByTokenHash: "novo-hash");

        token.RevokedAt.Should().Be(UtcNow.AddDays(1));
        token.ReplacedByTokenHash.Should().Be("novo-hash");
        token.IsActive(UtcNow.AddDays(1)).Should().BeFalse();
    }
}
