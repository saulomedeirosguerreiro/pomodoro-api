namespace Pomodoro.Domain.Enums;

/// <summary>Durações padrão (em segundos) de cada tipo de período, conforme Pomodoro.md §3.</summary>
public static class SessionTypeDurations
{
    public const int FocoSeconds = 25 * 60;
    public const int DescansoCurtoSeconds = 5 * 60;
    public const int DescansoLongoSeconds = 15 * 60;

    /// <summary>Tolerância máxima (em segundos) acima da duração padrão aceita ao registrar uma sessão (L-13).</summary>
    public const int ToleranceSeconds = 60;

    public static int StandardSecondsFor(SessionType type) => type switch
    {
        SessionType.Foco => FocoSeconds,
        SessionType.DescansoCurto => DescansoCurtoSeconds,
        SessionType.DescansoLongo => DescansoLongoSeconds,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de sessão desconhecido.")
    };

    public static int MaxAllowedSecondsFor(SessionType type) => StandardSecondsFor(type) + ToleranceSeconds;
}
