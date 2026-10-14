using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class AuthEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly HttpClient _client;
    private readonly PomodoroApiFactory _factory;

    public AuthEndpointsTests(PomodoroApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>Extrai só o par <c>nome=valor</c> de um header <c>Set-Cookie</c> (descarta Path/HttpOnly/etc).</summary>
    private static string ExtractCookiePair(string setCookieHeader) => setCookieHeader.Split(';')[0];

    [Fact]
    public async Task Register_ComDadosValidos_Retorna201SemSenha()
    {
        var email = $"joao-{Guid.NewGuid():N}@email.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "João",
            email,
            password = "Senha123",
            acceptedTerms = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("Senha123").And.NotContain("password");
    }

    [Fact]
    public async Task Register_ComEmailJaCadastrado_Retorna409()
    {
        var email = $"duplicado-{Guid.NewGuid():N}@email.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Outro João",
            email = email.ToUpperInvariant(),
            password = "Senha123",
            acceptedTerms = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ComSenhaForaDaRegra_Retorna422ComErroPorCampo()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "João",
            email = $"fraco-{Guid.NewGuid():N}@email.com",
            password = "123456",
            acceptedTerms = true
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Password");
    }

    [Fact]
    public async Task Register_SemAceitarOsTermos_Retorna422ComErroNoCampoAcceptedTerms()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "João",
            email = $"semtermos-{Guid.NewGuid():N}@email.com",
            password = "Senha123",
            acceptedTerms = false
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("AcceptedTerms");
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_Retorna200ComToken()
    {
        var email = $"login-ok-{Guid.NewGuid():N}@email.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("token");
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Retorna401ComMensagemGenerica()
    {
        var email = $"login-errado-{Guid.NewGuid():N}@email.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "SenhaErrada1" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_Retorna401ComMesmaMensagemDeSenhaErrada()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"naoexiste-{Guid.NewGuid():N}@email.com",
            password = "Senha123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_SetaCookieHttpOnlyDeRefreshToken()
    {
        var email = $"login-cookie-{Guid.NewGuid():N}@email.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("pomodoro_refresh=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Refresh_ComCookieValido_RetornaNovoAccessTokenERotacionaOCookie()
    {
        var email = $"refresh-ok-{Guid.NewGuid():N}@email.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });
        var loginCookie = loginResponse.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("pomodoro_refresh="));

        var response = await _client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("token");
        response.Headers.TryGetValues("Set-Cookie", out var newCookies).Should().BeTrue();
        newCookies!.Should().Contain(c => c.StartsWith("pomodoro_refresh=") && c != loginCookie);
    }

    [Fact]
    public async Task Refresh_SemCookie_Retorna401()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ComTokenJaRotacionado_DetectaReusoERevogaInclusiveATrocaNova()
    {
        // Client próprio, sem cookie jar automático: controla exatamente qual cookie vai em cada request,
        // o que permite reapresentar de propósito um cookie já rotacionado.
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var email = $"refresh-reuso-{Guid.NewGuid():N}@email.com";
        await client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });
        var loginCookie = ExtractCookiePair(loginResponse.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("pomodoro_refresh=")));

        using var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRefreshRequest.Headers.Add("Cookie", loginCookie);
        var firstRefresh = await client.SendAsync(firstRefreshRequest);
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedCookie = ExtractCookiePair(firstRefresh.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("pomodoro_refresh=")));

        // Reapresenta o cookie de login — já revogado pela rotação acima — simulando um token roubado.
        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        reuseRequest.Headers.Add("Cookie", loginCookie);
        var reuseResponse = await client.SendAsync(reuseRequest);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // O reuso derruba TODA a sessão — até o token que tinha acabado de ser emitido legitimamente
        // na rotação acima para de funcionar.
        using var afterReuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        afterReuseRequest.Headers.Add("Cookie", rotatedCookie);
        var afterReuseResponse = await client.SendAsync(afterReuseRequest);
        afterReuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevogaORefreshTokenELimpaOCookie()
    {
        var email = $"logout-{Guid.NewGuid():N}@email.com";
        await _client.PostAsJsonAsync(
            "/api/auth/register", new { name = "João", email, password = "Senha123", acceptedTerms = true });
        await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Senha123" });

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshAfterLogout = await _client.PostAsync("/api/auth/refresh", null);
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
