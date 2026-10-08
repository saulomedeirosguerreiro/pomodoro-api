using System.Text.RegularExpressions;
using FluentValidation;

namespace Pomodoro.Application.Auth.Register;

/// <summary>Regras L-2 (nome/e-mail) e D-Q3 (senha 8+ chars, 1 letra + 1 número, máx. 72 — limite do bcrypt).</summary>
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .Must(name => name.Trim().Length is >= 2 and <= 100)
            .WithMessage("Nome deve ter entre 2 e 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail em formato inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(72).WithMessage("Senha deve ter no máximo 72 caracteres.")
            .Must(password => Regex.IsMatch(password, "[A-Za-z]") && Regex.IsMatch(password, "[0-9]"))
            .WithMessage("Senha deve conter pelo menos 1 letra e 1 número.");
    }
}
