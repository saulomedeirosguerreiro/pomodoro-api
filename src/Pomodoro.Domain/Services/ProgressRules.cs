using Pomodoro.Domain.Enums;

namespace Pomodoro.Domain.Services;

/// <summary>
/// Regras puras de progresso (XP, Nível, Sementes, Streak) — G-Q6=A (projeção, sem saldo persistido),
/// G-Q7 (fórmulas) e G-Q10 (streak). Nenhuma I/O aqui; conversão de fuso horário é responsabilidade
/// de quem chama (Application), que converte UTC para a data local antes de usar <see cref="CalculateStreakDays"/>.
/// </summary>
public static class ProgressRules
{
    public const int FocusXp = 25;
    public const int RestXp = 5;
    public const int XpPerLevelMultiplier = 200;
    public const int SeedsPerFocus = 15;
    public const int SeedsPerCycleBonus = 10;

    public static int XpFor(SessionType type, SessionStatus status)
    {
        if (status != SessionStatus.Concluido)
        {
            return 0;
        }

        return type == SessionType.Foco ? FocusXp : RestXp;
    }

    /// <summary>Sementes por foco concluído, mais um bônus por pausa longa concluída (aproxima "ciclo completo").</summary>
    public static int SeedsFor(SessionType type, SessionStatus status)
    {
        if (status != SessionStatus.Concluido)
        {
            return 0;
        }

        return type switch
        {
            SessionType.Foco => SeedsPerFocus,
            SessionType.DescansoLongo => SeedsPerCycleBonus,
            _ => 0,
        };
    }

    public static (int Level, int XpInLevel, int XpForNextLevel) CalculateLevel(int totalXp)
    {
        var level = 1;
        var xpConsumed = 0;

        while (true)
        {
            var xpForThisLevel = XpPerLevelMultiplier * level;
            if (totalXp - xpConsumed < xpForThisLevel)
            {
                break;
            }

            xpConsumed += xpForThisLevel;
            level++;
        }

        var xpInLevel = totalXp - xpConsumed;
        var xpForNextLevel = XpPerLevelMultiplier * level;
        return (level, xpInLevel, xpForNextLevel);
    }

    public static string TitleFor(int level) => level switch
    {
        >= 12 => "Guardião do Pomar",
        >= 8 => "Mestre da Horta",
        >= 5 => "Horticultor Focado",
        >= 3 => "Jardineiro Produtivo",
        >= 2 => "Broto Aprendiz",
        _ => "Semente Curiosa",
    };

    /// <summary>
    /// Dias consecutivos com pelo menos um foco concluído, terminando hoje ou ontem (G-Q10) —
    /// só "quebra" quando o dia de hoje termina sem nenhum foco.
    /// </summary>
    public static int CalculateStreakDays(IReadOnlySet<DateOnly> activeDates, DateOnly today)
    {
        DateOnly cursor;
        if (activeDates.Contains(today))
        {
            cursor = today;
        }
        else if (activeDates.Contains(today.AddDays(-1)))
        {
            cursor = today.AddDays(-1);
        }
        else
        {
            return 0;
        }

        var streak = 0;
        while (activeDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
