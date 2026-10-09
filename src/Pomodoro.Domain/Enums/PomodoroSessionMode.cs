namespace Pomodoro.Domain.Enums;

/// <summary>
/// Origem opcional de uma <see cref="Entities.PomodoroSession"/>. `null` significa o modo clássico
/// (Pomodoro de ciclo fixo). Não é um enum porque hoje só existe um valor possível além de `null` —
/// um enum de 1 membro não ganharia nada sobre uma constante de string.
/// </summary>
public static class PomodoroSessionMode
{
    public const string Flexivel = "flexivel";
}
