using FluentValidation;
using Pomodoro.Application.Common;

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

        RuleFor(x => x.Password).MustBeAStrongPassword();

        RuleFor(x => x.AcceptedTerms)
            .Equal(true).WithMessage("É necessário aceitar os Termos de Uso e a Política de Privacidade.");
    }
}
