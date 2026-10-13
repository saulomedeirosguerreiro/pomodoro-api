using FluentValidation;
using Pomodoro.Application.Common;

namespace Pomodoro.Application.Users.ChangePassword;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Senha atual é obrigatória.");
        RuleFor(x => x.NewPassword).MustBeAStrongPassword();
    }
}
