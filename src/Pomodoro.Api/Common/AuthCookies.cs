namespace Pomodoro.Api.Common;

/// <summary>
/// Centraliza a config do cookie httpOnly do refresh token. Em dev, front (5173) e back (5134) estão
/// em <c>localhost</c> — portas diferentes mas mesmo site, então <c>SameSite=Lax</c> sem <c>Secure</c>
/// funciona sobre HTTP puro. Em produção, front (Vercel) e back (Render) são domínios diferentes —
/// exige <c>SameSite=None; Secure</c> para o browser aceitar o cookie cross-site.
/// </summary>
public static class AuthCookies
{
    public const string RefreshTokenCookieName = "pomodoro_refresh";
    private const string RefreshTokenCookiePath = "/api/auth";

    public static void SetRefreshToken(HttpContext httpContext, string rawRefreshToken, DateTimeOffset expiresAt)
    {
        httpContext.Response.Cookies.Append(RefreshTokenCookieName, rawRefreshToken, BuildOptions(httpContext, expiresAt));
    }

    public static void ClearRefreshToken(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(RefreshTokenCookieName, BuildOptions(httpContext, DateTimeOffset.UnixEpoch));
    }

    public static string? ReadRefreshToken(HttpContext httpContext) =>
        httpContext.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var value) ? value : null;

    private static CookieOptions BuildOptions(HttpContext httpContext, DateTimeOffset expiresAt)
    {
        var isDevelopment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
            Path = RefreshTokenCookiePath,
            Expires = expiresAt,
        };
    }
}
