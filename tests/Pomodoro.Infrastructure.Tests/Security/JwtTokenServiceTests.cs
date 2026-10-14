using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Security;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; init; }
    }

    [Fact]
    public void GenerateToken_IncluiClaimsDoUsuarioEExpiraEm24Horas()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedDateTimeProvider { UtcNow = now };
        var options = Options.Create(new JwtOptions { Secret = "uma-chave-secreta-de-teste-bem-grande-32+", ExpirationHours = 24 });
        var service = new JwtTokenService(options, clock);
        var user = User.Create("João", "joao@email.com", "hash", now);

        var token = service.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "joao@email.com");
        jwt.ValidTo.Should().BeCloseTo(now.AddHours(24), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GenerateRefreshToken_RetornaRawETokenAleatoriosComHashCorrespondenteEExpiraEm30Dias()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedDateTimeProvider { UtcNow = now };
        var options = Options.Create(new JwtOptions { Secret = "uma-chave-secreta-de-teste-bem-grande-32+", RefreshTokenExpirationDays = 30 });
        var service = new JwtTokenService(options, clock);

        var first = service.GenerateRefreshToken(now);
        var second = service.GenerateRefreshToken(now);

        first.RawToken.Should().NotBeNullOrWhiteSpace();
        first.RawToken.Should().NotBe(second.RawToken);
        first.Hash.Should().Be(service.HashRefreshToken(first.RawToken));
        first.ExpiresAt.Should().Be(now.AddDays(30));
    }

    [Fact]
    public void HashRefreshToken_EhDeterministicoParaOMesmoValor()
    {
        var options = Options.Create(new JwtOptions { Secret = "uma-chave-secreta-de-teste-bem-grande-32+" });
        var service = new JwtTokenService(options, new FixedDateTimeProvider { UtcNow = DateTime.UtcNow });

        service.HashRefreshToken("valor-fixo").Should().Be(service.HashRefreshToken("valor-fixo"));
        service.HashRefreshToken("valor-fixo").Should().NotBe(service.HashRefreshToken("outro-valor"));
    }
}
