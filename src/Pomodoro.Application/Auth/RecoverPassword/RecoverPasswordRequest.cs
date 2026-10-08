namespace Pomodoro.Application.Auth.RecoverPassword;

/// <summary>
/// Recuperação de senha sem e-mail (decisão de produto aceita pelo usuário): só troca a senha se
/// `Name` e `Email` baterem com um usuário existente — mais fraco que verificação por posse de e-mail,
/// mitigado por rate limit no endpoint (ver `RateLimitPolicies.PasswordRecovery`).
/// </summary>
public sealed record RecoverPasswordRequest(string Name, string Email, string NewPassword);
