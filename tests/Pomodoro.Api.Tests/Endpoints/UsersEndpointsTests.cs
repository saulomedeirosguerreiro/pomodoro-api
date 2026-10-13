using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class UsersEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;
    private readonly HttpClient _client;

    public UsersEndpointsTests(PomodoroApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ComTokenInvalido_Retorna401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-adulterado");

        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ComTokenValido_Retorna200ComPerfil()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "perfil");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(user.Email).And.Contain("completedSessions");
    }

    [Fact]
    public async Task GetPomodoros_SemToken_Retorna401()
    {
        var response = await _client.GetAsync("/api/pomodoros");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static HttpRequestMessage DeleteMeRequest(string password) => new(HttpMethod.Delete, "/api/users/me")
    {
        Content = JsonContent.Create(new { password }),
    };

    [Fact]
    public async Task DeleteMe_SemToken_Retorna401()
    {
        var response = await _client.SendAsync(DeleteMeRequest("qualquer"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMe_SemSenha_Retorna422()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "excluirsemsenha");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(DeleteMeRequest(""));

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task DeleteMe_ComSenhaErrada_Retorna401ENaoExclui()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "excluirsenhaerrada");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(DeleteMeRequest("senha-errada"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var meResponse = await _client.GetAsync("/api/users/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteMe_ComSenhaCorreta_Retorna204EApagaAConta()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "excluircomsucesso");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(DeleteMeRequest(user.Password));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var meResponse = await _client.GetAsync("/api/users/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMe_NaoAfetaOutroUsuario()
    {
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        var userA = await TestUserFactory.RegisterAndLoginAsync(clientA, "excluirusera");
        var userB = await TestUserFactory.RegisterAndLoginAsync(clientB, "excluiruserb");

        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userA.Token);
        await clientA.SendAsync(DeleteMeRequest(userA.Password));

        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userB.Token);
        var meResponseB = await clientB.GetAsync("/api/users/me");

        meResponseB.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpRequestMessage UpdateNameRequest(string name) => new(HttpMethod.Patch, "/api/users/me")
    {
        Content = JsonContent.Create(new { name }),
    };

    [Fact]
    public async Task UpdateName_SemToken_Retorna401()
    {
        var response = await _client.SendAsync(UpdateNameRequest("Novo Nome"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateName_ComNomeInvalido_Retorna422()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "trocarnomeinvalido");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(UpdateNameRequest("A"));

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task UpdateName_ComNomeValido_Retorna200EAtualizaOPerfil()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "trocarnomesucesso");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(UpdateNameRequest("Nome Atualizado"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Nome Atualizado");

        var meResponse = await _client.GetAsync("/api/users/me");
        var meBody = await meResponse.Content.ReadAsStringAsync();
        meBody.Should().Contain("Nome Atualizado");
    }

    private static HttpRequestMessage ChangePasswordRequest(string currentPassword, string newPassword) =>
        new(HttpMethod.Patch, "/api/users/me/password")
        {
            Content = JsonContent.Create(new { currentPassword, newPassword }),
        };

    [Fact]
    public async Task ChangePassword_SemToken_Retorna401()
    {
        var response = await _client.SendAsync(ChangePasswordRequest("qualquer", "SenhaNova123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_ComNovaSenhaForaDaRegra_Retorna422()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "trocarsenhainvalida");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(ChangePasswordRequest(user.Password, "curta"));

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task ChangePassword_ComSenhaAtualErrada_Retorna401ENaoAtualiza()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "trocarsenhaerrada");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(ChangePasswordRequest("senha-errada", "SenhaNova123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_ComSenhaAtualCorreta_Retorna204EPermiteLoginComANovaSenha()
    {
        var user = await TestUserFactory.RegisterAndLoginAsync(_client, "trocarsenhasucesso");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var response = await _client.SendAsync(ChangePasswordRequest(user.Password, "SenhaNova123"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var oldLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "SenhaNova123" });
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
