using FluentValidation;
using Pomodoro.Application.Common;

namespace Pomodoro.Application.Auth.RecoverPassword;

public sealed class RecoverPasswordValidator : AbstractValidator<RecoverPasswordRequest>
{
    public RecoverPasswordValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Nome é obrigatório.");

        RuleFor(x => x.Email).NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail em formato inválido.");

        RuleFor(x => x.NewPassword).MustBeAStrongPassword();
    }
}
