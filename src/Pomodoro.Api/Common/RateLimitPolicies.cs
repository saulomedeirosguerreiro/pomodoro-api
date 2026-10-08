using System.Net;

namespace Pomodoro.Api.Common;

/// <summary>Nomes de políticas de rate limit nativo do .NET 8 (<c>AddRateLimiter</c> em <c>Program.cs</c>).</summary>
public static class RateLimitPolicies
{
    public const string PasswordRecovery = "password-recovery";

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
