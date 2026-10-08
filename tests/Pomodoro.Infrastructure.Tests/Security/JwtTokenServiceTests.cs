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
}
