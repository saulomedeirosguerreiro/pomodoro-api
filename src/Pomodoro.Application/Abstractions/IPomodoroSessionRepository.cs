using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface IPomodoroSessionRepository
{
    Task AddAsync(PomodoroSession session, CancellationToken cancellationToken);

    Task<PomodoroSession?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<PomodoroSession> Items, int TotalCount)> ListForUserAsync(
        int userId, int limit, int offset, CancellationToken cancellationToken);

    Task<int> CountCompletedFocusAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Existe outra sessão do mesmo usuário cujo intervalo [startedAt, completedAt] se sobrepõe ao informado? (US-41)</summary>
    Task<bool> ExistsOverlappingAsync(int userId, DateTime startedAt, DateTime completedAt, CancellationToken cancellationToken);

    /// <summary>Todas as sessões do usuário, sem paginação — usado para calcular XP/Nível/Sementes/Streak (US-40).</summary>
    Task<IReadOnlyList<PomodoroSession>> ListAllAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Quantos focos concluídos cada tarefa tem, para as tarefas informadas (US-48 RN-03).</summary>
    Task<IReadOnlyDictionary<int, int>> CountCompletedByTaskAsync(
        IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken);
}
