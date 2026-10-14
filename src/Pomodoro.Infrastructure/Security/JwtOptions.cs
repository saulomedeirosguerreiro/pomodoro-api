namespace Pomodoro.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Segredo usado para assinar o token. Obrigatório — a Api falha ao subir sem ele (RNF-02).</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Validade do access token em horas (Q-15, revisada: 24h + refresh token).</summary>
    public int ExpirationHours { get; set; } = 24;

    /// <summary>Validade do refresh token em dias (Q-15, revisada: padrão 30 dias).</summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
