using Pomodoro.Domain.Common;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Domain.Entities;

public sealed class PomodoroSession
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public SessionType Type { get; private set; }
    public SessionStatus Status { get; private set; }
    public int DurationSeconds { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Tarefa vinculada (opcional, só para foco — US-50). Nula quando a tarefa é excluída.</summary>
    public int? TaskItemId { get; private set; }

    private PomodoroSession()
    {
    }

    public static PomodoroSession Create(
        int userId,
        SessionType type,
        SessionStatus status,
        int durationSeconds,
        DateTime startedAt,
        DateTime completedAt,
        DateTime utcNow,
        int? taskItemId = null)
    {
        if (userId <= 0)
        {
            throw new DomainException("Sessão precisa pertencer a um usuário válido.");
        }

        if (durationSeconds <= 0)
        {
            throw new DomainException("Duração da sessão precisa ser maior que zero.");
        }

        var maxAllowed = SessionTypeDurations.MaxAllowedSecondsFor(type);
        if (durationSeconds > maxAllowed)
        {
            throw new DomainException(
                $"Duração de {durationSeconds}s excede o máximo permitido de {maxAllowed}s para o tipo {type}.");
        }

        if (completedAt < startedAt)
        {
            throw new DomainException("Data de conclusão não pode ser anterior à data de início.");
        }

        if (taskItemId.HasValue && type != SessionType.Foco)
        {
            throw new DomainException("Só sessões de foco podem ser vinculadas a uma tarefa.");
        }

        return new PomodoroSession
        {
            UserId = userId,
            Type = type,
            Status = status,
            DurationSeconds = durationSeconds,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            CreatedAt = utcNow,
            TaskItemId = taskItemId,
        };
    }
}
