using Pomodoro.Domain.Enums;

namespace Pomodoro.Domain.Services;

/// <summary>
/// Sugere o próximo tipo de período ao final de um período concluído (US-12 RN-02, D-Q5).
/// Puramente funcional: não inicia nada sozinho, apenas calcula a sugestão.
/// </summary>
public static class PomodoroCycleAdvisor
{
    private const int FociPerLongBreak = 4;

    /// <summary>
    /// </summary>
    /// <param name="completedType">Tipo do período que acabou de ser concluído.</param>
    /// <param name="totalFociCompletedSoFar">
    /// Total histórico de focos concluídos pelo usuário, já contando o período atual se ele for um foco.
    /// </param>
    public static SessionType NextSuggestedType(SessionType completedType, int totalFociCompletedSoFar)
    {
        if (completedType != SessionType.Foco)
        {
            return SessionType.Foco;
        }

        var isFourthFocusInCycle = totalFociCompletedSoFar > 0 && totalFociCompletedSoFar % FociPerLongBreak == 0;
        return isFourthFocusInCycle ? SessionType.DescansoLongo : SessionType.DescansoCurto;
    }
}
