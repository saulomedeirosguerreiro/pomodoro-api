using FluentValidation;

namespace Pomodoro.Application.Users.UpdateName;

/// <summary>Mesma regra de nome usada no cadastro (L-2): 2 a 100 caracteres.</summary>
public sealed class UpdateNameValidator : AbstractValidator<UpdateNameRequest>
{
    public UpdateNameValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .Must(name => name.Trim().Length is >= 2 and <= 100)
            .WithMessage("Nome deve ter entre 2 e 100 caracteres.");
    }
}
