namespace Pomodoro.Application.Users.ChangePassword;

/// <summary>Exige a senha atual para confirmar, mesmo espírito de <see cref="Users.Delete.DeleteAccountRequest"/>.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
