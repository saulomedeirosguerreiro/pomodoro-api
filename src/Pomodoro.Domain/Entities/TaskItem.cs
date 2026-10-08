using Pomodoro.Domain.Common;
using Pomodoro.Domain.Enums;

namespace Pomodoro.Domain.Entities;

public sealed class TaskItem
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 500;
    public const int MinEstimatedPomodoros = 1;
    public const int MaxEstimatedPomodoros = 20;

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskPriority Priority { get; private set; }
    public int EstimatedPomodoros { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TaskItem()
    {
    }

    public static TaskItem Create(
        int userId, string title, string? description, TaskPriority priority, int estimatedPomodoros, DateTime utcNow)
    {
        if (userId <= 0)
        {
            throw new DomainException("Tarefa precisa pertencer a um usuário válido.");
        }

        var task = new TaskItem
        {
            UserId = userId,
            Status = TaskItemStatus.AFazer,
            CreatedAt = utcNow,
        };

        task.UpdateDetails(title, description, priority, estimatedPomodoros, utcNow);
        return task;
    }

    public void UpdateDetails(
        string title, string? description, TaskPriority priority, int estimatedPomodoros, DateTime utcNow)
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateEstimatedPomodoros(estimatedPomodoros);

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        EstimatedPomodoros = estimatedPomodoros;
        UpdatedAt = utcNow;
    }

    /// <summary>Coloca a tarefa em foco (G-Q13a: no máximo uma por usuário — garantido pela Application).</summary>
    public void MarkAsFocused(DateTime utcNow)
    {
        if (Status == TaskItemStatus.Feito)
        {
            throw new DomainException("Uma tarefa concluída não pode voltar a ficar em foco.");
        }

        Status = TaskItemStatus.EmCurso;
        UpdatedAt = utcNow;
    }

    public void MarkAsTodo(DateTime utcNow)
    {
        Status = TaskItemStatus.AFazer;
        UpdatedAt = utcNow;
    }

    public void MarkAsDone(DateTime utcNow)
    {
        Status = TaskItemStatus.Feito;
        UpdatedAt = utcNow;
    }

    private static void ValidateTitle(string title)
    {
        var trimmedLength = title?.Trim().Length ?? 0;
        if (trimmedLength is < 1 or > TitleMaxLength)
        {
            throw new DomainException($"Título deve ter entre 1 e {TitleMaxLength} caracteres.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is { Length: > DescriptionMaxLength })
        {
            throw new DomainException($"Descrição deve ter no máximo {DescriptionMaxLength} caracteres.");
        }
    }

    private static void ValidateEstimatedPomodoros(int estimatedPomodoros)
    {
        if (estimatedPomodoros is < MinEstimatedPomodoros or > MaxEstimatedPomodoros)
        {
            throw new DomainException(
                $"Estimativa de pomodoros deve ser entre {MinEstimatedPomodoros} e {MaxEstimatedPomodoros}.");
        }
    }
}
