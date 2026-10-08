using FluentValidation;
using Pomodoro.Application.Common;

namespace Pomodoro.Application.Tasks.SetStatus;

public sealed class SetTaskStatusValidator : AbstractValidator<SetTaskStatusRequest>
{
    public SetTaskStatusValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => TaskWireFormat.TryParseStatus(status, out _))
            .WithMessage("Status de tarefa inválido. Use a_fazer, em_curso ou feito.");
    }
}
