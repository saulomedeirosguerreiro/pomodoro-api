using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

/// <summary>
/// `POST /api/auth/register` tem rate limit nativo do .NET 8 (50 tentativas/15 min por IP, partição
/// fixa — ver `RateLimitPolicies.Register`), mitigando enumeração de e-mails via o 409 "E-mail já
/// cadastrado.". Fixture isolada por teste, pelo mesmo motivo documentado em
/// <see cref="PasswordRecoveryEndpointsTests"/>: o rate limiter acumula estado pela vida da
/// <see cref="PomodoroApiFactory"/>, então uma fixture compartilhada com outros testes de registro
/// contaminaria o orçamento de tentativas.
/// </summary>
public sealed class RegisterRateLimitTests
{
    [Fact]
    public async Task Register_ExcedeLimiteDeTentativasDoMesmoIp_Retorna429ComEnvelopeDeErro()
    {
        using var factory = new PomodoroApiFactory();
        var client = factory.CreateClient();
        const int permitLimit = 50;

        HttpResponseMessage? last = null;
        for (var i = 0; i < permitLimit + 1; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Rate Limit",
                email = $"rate-limit-register-{Guid.NewGuid():N}@email.com",
                password = "Senha123",
                acceptedTerms = true
            });
        }

        last!.StatusCode.Should().Be((HttpStatusCode)429);
        var body = await last.Content.ReadAsStringAsync();
        body.Should().Contain("rate_limited");
    }
}
