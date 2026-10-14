namespace Pomodoro.Application.Auth.Login;

/// <summary>
/// Resultado interno do <see cref="LoginHandler"/> — o endpoint usa <c>RefreshToken</c>/<c>RefreshTokenExpiresAt</c>
/// para setar o cookie httpOnly e devolve ao cliente só <see cref="LoginResponse"/> (sem o refresh token no corpo).
/// </summary>
public sealed record LoginResult(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt, LoginUserSummary User);
