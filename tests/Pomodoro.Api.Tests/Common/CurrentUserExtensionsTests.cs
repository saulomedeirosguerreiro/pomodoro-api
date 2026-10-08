using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Pomodoro.Api.Common;
using Xunit;

namespace Pomodoro.Api.Tests.Common;

public class CurrentUserExtensionsTests
{
    [Fact]
    public void GetUserId_ComClaimSub_RetornaOId()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, "42") });
        var principal = new ClaimsPrincipal(identity);

        principal.GetUserId().Should().Be(42);
    }

    [Fact]
    public void GetUserId_SemClaimSub_LancaInvalidOperationException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var act = () => principal.GetUserId();

        act.Should().Throw<InvalidOperationException>();
    }
}
