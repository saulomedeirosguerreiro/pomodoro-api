using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Common;

/// <summary>Tradução entre os enums de domínio de tarefas e os literais de texto do contrato HTTP (G-Q13a).</summary>
public static class TaskWireFormat
{
    public static bool TryParsePriority(string? value, out TaskPriority priority)
    {
        switch (value)
        {
            case "baixa":
                priority = TaskPriority.Baixa;
                return true;
            case "media":
                priority = TaskPriority.Media;
                return true;
            case "alta":
                priority = TaskPriority.Alta;
                return true;
            default:
                priority = default;
                return false;
        }
    }

    public static string ToWire(this TaskPriority priority) => priority switch
    {
        TaskPriority.Baixa => "baixa",
        TaskPriority.Media => "media",
        TaskPriority.Alta => "alta",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Prioridade desconhecida."),
    };

    public static bool TryParseStatus(string? value, out TaskItemStatus status)
    {
        switch (value)
        {
            case "a_fazer":
                status = TaskItemStatus.AFazer;
                return true;
            case "em_curso":
                status = TaskItemStatus.EmCurso;
                return true;
            case "feito":
                status = TaskItemStatus.Feito;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static string ToWire(this TaskItemStatus status) => status switch
    {
        TaskItemStatus.AFazer => "a_fazer",
        TaskItemStatus.EmCurso => "em_curso",
        TaskItemStatus.Feito => "feito",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status de tarefa desconhecido."),
    };
}
