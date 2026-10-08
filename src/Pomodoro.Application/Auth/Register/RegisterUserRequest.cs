namespace Pomodoro.Application.Auth.Register;

public sealed record RegisterUserRequest(string Name, string Email, string Password);
