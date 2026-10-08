using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

/// <summary>
/// `POST /api/auth/password-recovery` tem rate limit nativo do .NET 8 (5 tentativas/15 min por IP,
/// partição fixa — ver `RateLimitPolicies.PasswordRecovery`). Diferente de `AuthEndpointsTests`
/// (que compartilha UMA <see cref="PomodoroApiFactory"/> via <c>IClassFixture</c> entre todos os seus
/// testes), esta classe cria uma <see cref="PomodoroApiFactory"/> NOVA por teste: como o rate limiter
/// acumula estado pela vida inteira do processo da Api (não reseta entre `[Fact]`s do mesmo fixture),
/// compartilhar uma única instância faria os testes "normais" (sucesso/404/422) consumirem parte do
/// orçamento de 5 tentativas e contaminarem o teste de 429 — ou pior, fazer um teste comum falhar com
/// 429 dependendo da ordem de execução. Isolamento total por teste elimina esse acoplamento.
/// </summary>
public sealed class PasswordRecoveryEndpointsTests
{
    [Fact]
    public async Task RecuperarSenha_ComNomeEEmailCorretos_PermiteLoginComNovaSenhaENaoComAAntiga()
    {
        using var factory = new PomodoroApiFactory();
        var client = factory.CreateClient();
        var email = $"recuperar-{Guid.NewGuid():N}@email.com";
        await client.PostAsJsonAsync("/api/auth/register", new { name = "João", email, password = "Senha123" });

        var response = await client.PostAsJsonAsync("/api/auth/password-recovery", new
        {
            name = "João",
            email,
            newPassword = "NovaSenha456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginComNova = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NovaSenha456" });
        loginComNova.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginComAntiga = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });
        loginComAntiga.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecuperarSenha_ComEmailInexistente_Retorna404ComMensagemGenerica()
    {
        using var factory = new PomodoroApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-recovery", new
        {
            name = "Ninguém",
            email = $"naoexiste-{Guid.NewGuid():N}@email.com",
            newPassword = "NovaSenha456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Não foi possível localizar uma conta com esses dados.");
    }

    [Fact]
    public async Task RecuperarSenha_ComNomeErrado_Retorna404ComMensagemGenericaIgualAoCasoDeEmailInexistente()
    {
        using var factoryNomeErrado = new PomodoroApiFactory();
        var clientNomeErrado = factoryNomeErrado.CreateClient();
        var email = $"nomeerrado-{Guid.NewGuid():N}@email.com";
        await clientNomeErrado.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123" });

        var responseNomeErrado = await clientNomeErrado.PostAsJsonAsync("/api/auth/password-recovery", new
        {
            name = "Nome Errado",
            email,
            newPassword = "NovaSenha456"
        });

        using var factoryEmailInexistente = new PomodoroApiFactory();
        var clientEmailInexistente = factoryEmailInexistente.CreateClient();
        var responseEmailInexistente = await clientEmailInexistente.PostAsJsonAsync("/api/auth/password-recovery", new
        {
            name = "João",
            email = $"naoexiste-{Guid.NewGuid():N}@email.com",
            newPassword = "NovaSenha456"
        });

        responseNomeErrado.StatusCode.Should().Be(HttpStatusCode.NotFound);
        responseEmailInexistente.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpoNomeErrado = await responseNomeErrado.Content.ReadAsStringAsync();
        var corpoEmailInexistente = await responseEmailInexistente.Content.ReadAsStringAsync();
        corpoNomeErrado.Should().Be(corpoEmailInexistente);
    }

    [Fact]
    public async Task RecuperarSenha_ComSenhaFracaOuCamposVazios_Retorna422()
    {
        using var factory = new PomodoroApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/password-recovery", new
        {
            name = "",
            email = "nao-e-email",
            newPassword = "fraca"
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task RecuperarSenha_ExcedeLimiteDeTentativasDoMesmoIp_Retorna429ComEnvelopeDeErro()
    {
        using var factory = new PomodoroApiFactory();
        var client = factory.CreateClient();
        const int permitLimit = 5;

        HttpResponseMessage? last = null;
        for (var i = 0; i < permitLimit + 1; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/password-recovery", new
            {
                name = "Ninguém",
                email = $"rate-limit-{Guid.NewGuid():N}@email.com",
                newPassword = "NovaSenha456"
            });
        }

        last!.StatusCode.Should().Be((HttpStatusCode)429);
        var body = await last.Content.ReadAsStringAsync();
        body.Should().Contain("rate_limited");
    }
}
