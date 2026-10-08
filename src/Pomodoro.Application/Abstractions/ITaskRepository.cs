using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Abstractions;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<TaskItem?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskItem>> ListForUserAsync(int userId, TaskItemStatus? status, CancellationToken cancellationToken);

    /// <summary>A tarefa em foco do usuário (no máximo uma, G-Q13a), se houver.</summary>
    Task<TaskItem?> FindInFocusAsync(int userId, CancellationToken cancellationToken);

    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);

    /// <summary>Persiste alterações feitas em entidades já rastreadas (ex.: após chamar um método de domínio).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
