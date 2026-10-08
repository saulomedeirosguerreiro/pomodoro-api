using Pomodoro.Domain.Enums;

namespace Pomodoro.Application.Common;

/// <summary>Tradução entre os enums de domínio e os literais de texto do contrato HTTP (I-3).</summary>
public static class SessionWireFormat
{
    public static bool TryParseType(string? value, out SessionType type)
    {
        switch (value)
        {
            case "foco":
                type = SessionType.Foco;
                return true;
            case "descanso_curto":
                type = SessionType.DescansoCurto;
                return true;
            case "descanso_longo":
                type = SessionType.DescansoLongo;
                return true;
            default:
                type = default;
                return false;
        }
    }

    public static string ToWire(this SessionType type) => type switch
    {
        SessionType.Foco => "foco",
        SessionType.DescansoCurto => "descanso_curto",
        SessionType.DescansoLongo => "descanso_longo",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de sessão desconhecido.")
    };

    public static bool TryParseStatus(string? value, out SessionStatus status)
    {
        switch (value)
        {
            case "concluido":
                status = SessionStatus.Concluido;
                return true;
            case "interrompido":
                status = SessionStatus.Interrompido;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static string ToWire(this SessionStatus status) => status switch
    {
        SessionStatus.Concluido => "concluido",
        SessionStatus.Interrompido => "interrompido",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status de sessão desconhecido.")
    };
}
