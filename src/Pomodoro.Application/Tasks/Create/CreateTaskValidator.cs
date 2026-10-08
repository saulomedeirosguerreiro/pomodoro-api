using FluentValidation;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Tasks.Create;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MaximumLength(TaskItem.TitleMaxLength)
            .WithMessage($"Título deve ter no máximo {TaskItem.TitleMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength)
            .WithMessage($"Descrição deve ter no máximo {TaskItem.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.Priority)
            .Must(priority => TaskWireFormat.TryParsePriority(priority, out _))
            .WithMessage("Prioridade inválida. Use baixa, media ou alta.");

        RuleFor(x => x.EstimatedPomodoros)
            .InclusiveBetween(TaskItem.MinEstimatedPomodoros, TaskItem.MaxEstimatedPomodoros)
            .WithMessage(
                $"Estimativa de pomodoros deve ser entre {TaskItem.MinEstimatedPomodoros} e {TaskItem.MaxEstimatedPomodoros}.");
    }
}
