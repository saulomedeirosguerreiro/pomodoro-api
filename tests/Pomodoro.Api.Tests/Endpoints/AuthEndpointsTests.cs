using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class AuthEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(PomodoroApiFactory factory)
    {
        _client = factory.CreateClient();
    }

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
}
