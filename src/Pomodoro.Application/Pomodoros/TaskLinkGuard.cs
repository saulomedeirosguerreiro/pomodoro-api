using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Pomodoros;

/// <summary>
/// US-50: só aceita o vínculo tarefa→sessão quando a tarefa é do mesmo usuário, a sessão é de foco
/// e a tarefa não está concluída. Mensagem genérica em qualquer um desses casos, para não revelar a
/// existência de tarefas de outros usuários (L-11). Compartilhado entre a criação avulsa
/// (<see cref="Create.CreatePomodoroHandler"/>) e a importação em lote.
/// </summary>
public static class TaskLinkGuard
{
    public const string GenericMessage = "Tarefa não encontrada ou indisponível para vínculo.";

    public static async Task EnsureCanBeLinkedAsync(
        ITaskRepository tasks, int userId, int taskId, SessionType type, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdForUserAsync(taskId, userId, cancellationToken);
        if (task is null || type != SessionType.Foco || task.Status == TaskItemStatus.Feito)
        {
            throw new FieldValidationException("TaskId", GenericMessage);
        }
    }
}
