using FluentValidation;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Pomodoros.Create;

/// <summary>
/// Regras L-13 (type/status restritos à lista, duration > 0 e dentro da faixa plausível por tipo,
/// completedAt >= startedAt) mais o antifraude da US-41: nem início nem fim podem estar no futuro
/// (com 60s de tolerância de relógio). A checagem de sobreposição com outras sessões do usuário
/// fica no handler (precisa do userId do token).
/// </summary>
public sealed class CreatePomodoroValidator : AbstractValidator<CreatePomodoroRequest>
{
    private const int FutureToleranceSeconds = 60;

    public CreatePomodoroValidator(IDateTimeProvider clock)
    {
        RuleFor(x => x.Type)
            .Must(type => SessionWireFormat.TryParseType(type, out _))
            .WithMessage("Tipo de sessão inválido. Use foco, descanso_curto ou descanso_longo.");

        RuleFor(x => x.Status)
            .Must(status => SessionWireFormat.TryParseStatus(status, out _))
            .WithMessage("Status de sessão inválido. Use concluido ou interrompido.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0).WithMessage("Duração deve ser maior que zero.");

        RuleFor(x => x)
            .Must(HaveDurationWithinAllowedRange)
            .WithName("DurationSeconds")
            .WithMessage("Duração fora da faixa permitida para o tipo informado.")
            .When(x => SessionWireFormat.TryParseType(x.Type, out _) && x.DurationSeconds > 0);

        RuleFor(x => x.CompletedAt)
            .GreaterThanOrEqualTo(x => x.StartedAt)
            .WithMessage("Data de conclusão não pode ser anterior à data de início.");

        RuleFor(x => x.StartedAt)
            .Must(startedAt => startedAt <= clock.UtcNow.AddSeconds(FutureToleranceSeconds))
            .WithMessage("Data de início não pode estar no futuro.");

        RuleFor(x => x.CompletedAt)
            .Must(completedAt => completedAt <= clock.UtcNow.AddSeconds(FutureToleranceSeconds))
            .WithMessage("Data de conclusão não pode estar no futuro.");

        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("TaskId inválido.")
            .When(x => x.TaskId.HasValue);

        RuleFor(x => x.Mode)
            .Must(mode => mode == PomodoroSessionMode.Flexivel)
            .WithMessage($"Modo de sessão inválido. Use {PomodoroSessionMode.Flexivel} ou não informe.")
            .When(x => x.Mode is not null);

        RuleFor(x => x.PlannedDurationSeconds)
            .GreaterThan(0).WithMessage("Duração planejada precisa ser maior que zero.")
            .When(x => x.PlannedDurationSeconds.HasValue);

        RuleFor(x => x.AddedSeconds)
            .GreaterThanOrEqualTo(0).WithMessage("Tempo adicionado não pode ser negativo.")
            .When(x => x.AddedSeconds.HasValue);
    }

    private static bool HaveDurationWithinAllowedRange(CreatePomodoroRequest request)
    {
        SessionWireFormat.TryParseType(request.Type, out var type);
        return request.DurationSeconds >= SessionTypeDurations.MinAllowedSecondsFor(type)
            && request.DurationSeconds <= SessionTypeDurations.MaxAllowedSecondsFor(type);
    }
}
