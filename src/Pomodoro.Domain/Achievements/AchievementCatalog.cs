namespace Pomodoro.Domain.Achievements;

/// <summary>Catálogo fixo de conquistas (G-Q14, US-53 RN-01) — mudar aqui nunca exige migração.</summary>
public static class AchievementCatalog
{
    public static readonly IReadOnlyList<AchievementDefinition> All = new List<AchievementDefinition>
    {
        new(
            "primeira_semente",
            "Primeira Semente",
            "Conclua seu primeiro foco.",
            s => s.CompletedFocusCount >= 1,
            _ => null),
        new(
            "ciclo_completo",
            "Ciclo Completo",
            "Conclua 4 focos e uma pausa longa.",
            s => s.CompletedFocusCount >= 4 && s.CompletedLongBreakCount >= 1,
            _ => null),
        new(
            "dia_fertil",
            "Dia Fértil",
            "Conclua 8 focos em um único dia.",
            s => s.MaxFocusInOneDay >= 8,
            s => new AchievementProgress(Math.Min(s.MaxFocusInOneDay, 8), 8)),
        new(
            "constancia_3",
            "Constância de 3 dias",
            "Mantenha uma sequência de 3 dias seguidos com foco.",
            s => s.LongestStreakDays >= 3,
            s => new AchievementProgress(Math.Min(s.LongestStreakDays, 3), 3)),
        new(
            "constancia_7",
            "Constância de 7 dias",
            "Mantenha uma sequência de 7 dias seguidos com foco.",
            s => s.LongestStreakDays >= 7,
            s => new AchievementProgress(Math.Min(s.LongestStreakDays, 7), 7)),
        new(
            "constancia_30",
            "Constância de 30 dias",
            "Mantenha uma sequência de 30 dias seguidos com foco.",
            s => s.LongestStreakDays >= 30,
            s => new AchievementProgress(Math.Min(s.LongestStreakDays, 30), 30)),
        new(
            "primeira_colheita",
            "Primeira Colheita",
            "Conclua sua primeira tarefa.",
            s => s.CompletedTasksCount >= 1,
            _ => null),
        new(
            "cem_tomates",
            "Cem Tomates",
            "Conclua 100 focos.",
            s => s.CompletedFocusCount >= 100,
            s => new AchievementProgress(Math.Min(s.CompletedFocusCount, 100), 100)),
        new(
            "nivel_5",
            "Nível 5",
            "Chegue ao nível 5.",
            s => s.Level >= 5,
            s => new AchievementProgress(Math.Min(s.Level, 5), 5)),
        new(
            "descanso_sabio",
            "Descanso Sábio",
            "Conclua 10 pausas.",
            s => s.CompletedRestCount >= 10,
            s => new AchievementProgress(Math.Min(s.CompletedRestCount, 10), 10)),
    };
}
