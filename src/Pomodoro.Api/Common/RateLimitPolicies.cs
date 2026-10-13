using System.Net;

namespace Pomodoro.Api.Common;

/// <summary>Nomes de políticas de rate limit nativo do .NET 8 (<c>AddRateLimiter</c> em <c>Program.cs</c>).</summary>
public static class RateLimitPolicies
{
    public const string PasswordRecovery = "password-recovery";

    /// <summary>
    /// Mitigação contra enumeração de e-mails via `/api/auth/register`: sem isso, o 409 "E-mail já
    /// cadastrado." vs. 201/422 vira um oráculo de velocidade ilimitada para varrer listas de e-mails.
    /// Orçamento bem mais folgado que <see cref="PasswordRecovery"/> (50/15 min por IP, não 5/15 min):
    /// cadastro é uma ação legítima de alta frequência por IP compartilhado (NAT de escritório/escola),
    /// diferente de recuperação de senha. Ainda corta um varredor automático de "ilimitado" para
    /// "no máximo 50 tentativas a cada 15 min por IP".
    /// </summary>
    public const string Register = "register";

    /// <summary>
    /// Chave de partição por IP do rate limiter — extraída como função pura testável (em vez de uma
    /// expressão inline em <c>Program.cs</c>) porque o host de testes em memória (<c>TestServer</c>)
    /// nunca preenche <see cref="System.Net.Sockets.Socket.RemoteEndPoint"/>/`RemoteIpAddress`, o que
    /// tornaria o branch do `??` impossível de cobrir a partir de um teste de integração via
    /// <c>WebApplicationFactory</c>.
    /// </summary>
    public static string ResolveClientPartitionKey(IPAddress? remoteIpAddress) =>
        remoteIpAddress?.ToString() ?? "unknown";
}
