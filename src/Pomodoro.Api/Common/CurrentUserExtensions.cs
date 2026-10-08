using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Pomodoro.Api.Common;

public static class CurrentUserExtensions
{
    /// <summary>Id do usuário autenticado, lido sempre do token (nunca do corpo/query) — RNF-03.</summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Token sem claim 'sub'.");

        return int.Parse(subject);
    }
}
