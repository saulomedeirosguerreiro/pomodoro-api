using System.Text.RegularExpressions;
using FluentValidation;

namespace Pomodoro.Application.Common;

/// <summary>
/// Regra D-Q3 (senha 8+ chars, 1 letra + 1 número, máx. 72 — limite do bcrypt), compartilhada entre
/// <see cref="Auth.Register.RegisterUserValidator"/> e o validator de recuperação de senha.
/// </summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> MustBeAStrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(72).WithMessage("Senha deve ter no máximo 72 caracteres.")
            .Must(password => Regex.IsMatch(password, "[A-Za-z]") && Regex.IsMatch(password, "[0-9]"))
            .WithMessage("Senha deve conter pelo menos 1 letra e 1 número.");
}
