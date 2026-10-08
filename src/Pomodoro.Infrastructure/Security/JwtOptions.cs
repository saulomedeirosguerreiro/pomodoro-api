namespace Pomodoro.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Segredo usado para assinar o token. Obrigatório — a Api falha ao subir sem ele (RNF-02).</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Validade do token em horas (Q-15: 24h, sem refresh).</summary>
    public int ExpirationHours { get; set; } = 24;
}
