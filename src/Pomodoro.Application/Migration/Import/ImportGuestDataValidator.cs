using FluentValidation;

namespace Pomodoro.Application.Migration.Import;

/// <summary>
/// Guarda de borda do lote inteiro — rejeita a requisição toda (não é o "pula o item" de conflito,
/// que é regra de negócio resolvida item a item no handler). Os tetos de tamanho existem porque a
/// avaliação de conquistas histórica (<see cref="Pomodoro.Domain.Achievements.AchievementTimelineResolver"/>)
/// é O(N log N): sem teto, um payload gigante vira vetor de negação de serviço.
/// </summary>
public sealed class ImportGuestDataValidator : AbstractValidator<ImportGuestDataRequest>
{
    public const int MaxTasks = 1000;
    public const int MaxSessions = 5000;

    public ImportGuestDataValidator()
    {
        RuleFor(x => x.GuestId)
            .NotEmpty().WithMessage("GuestId é obrigatório.")
            .MaximumLength(100).WithMessage("GuestId deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Tasks)
            .Must(tasks => tasks.Count <= MaxTasks)
            .WithMessage($"Lote de tarefas excede o limite de {MaxTasks} itens.");

        RuleFor(x => x.Sessions)
            .Must(sessions => sessions.Count <= MaxSessions)
            .WithMessage($"Lote de sessões excede o limite de {MaxSessions} itens.");
    }
}
