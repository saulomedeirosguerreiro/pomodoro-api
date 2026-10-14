namespace Pomodoro.Application.Auth.Refresh;

public sealed record RefreshResult(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
