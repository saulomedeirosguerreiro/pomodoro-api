namespace Pomodoro.Domain.Enums;

/// <summary>Durações padrão (em segundos) de cada tipo de período, conforme Pomodoro.md §3.</summary>
public static class SessionTypeDurations
{
    public const int FocoSeconds = 25 * 60;
    public const int DescansoCurtoSeconds = 5 * 60;
    public const int DescansoLongoSeconds = 15 * 60;

    public static int StandardSecondsFor(SessionType type) => type switch
    {
        SessionType.Foco => FocoSeconds,
        SessionType.DescansoCurto => DescansoCurtoSeconds,
        SessionType.DescansoLongo => DescansoLongoSeconds,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de sessão desconhecido.")
    };

    /// <summary>
    /// Janela de duração aceita por tipo (RN atualizada): o usuário pode customizar os tempos de
    /// foco/pausa no frontend, então o backend não valida mais contra o padrão fixo ± tolerância —
    /// só garante que o valor enviado é plausível para aquele tipo de período. Os limites espelham
    /// as mesmas faixas que a UI de Configurações permite editar (frontend/src/routes/ConfiguracoesPage.tsx).
    /// </summary>
    public static int MinAllowedSecondsFor(SessionType type) => type switch
    {
        SessionType.Foco => 5 * 60,
        SessionType.DescansoCurto => 1 * 60,
        SessionType.DescansoLongo => 5 * 60,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de sessão desconhecido.")
    };

    public static int MaxAllowedSecondsFor(SessionType type) => type switch
    {
        SessionType.Foco => 120 * 60,
        SessionType.DescansoCurto => 30 * 60,
        SessionType.DescansoLongo => 60 * 60,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de sessão desconhecido.")
    };
}
