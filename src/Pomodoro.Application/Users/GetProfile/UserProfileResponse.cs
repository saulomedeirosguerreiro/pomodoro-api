namespace Pomodoro.Application.Users.GetProfile;

public sealed record UserProfileResponse(int Id, string Name, string Email, int CompletedSessions);
