namespace Pomodoro.Application.Auth.Login;

public sealed record LoginResponse(string Token, LoginUserSummary User);

public sealed record LoginUserSummary(int Id, string Name, string Email);
