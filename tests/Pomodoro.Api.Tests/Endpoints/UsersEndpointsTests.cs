using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class UsersEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(PomodoroApiFactory factory)
    {
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
}
