namespace Pomodoro.Application.Users.Delete;

/// <summary>Exige a senha atual para confirmar — ação irreversível (RNF-02, mesmo espírito de L-13).</summary>
public sealed record DeleteAccountRequest(string Password);
