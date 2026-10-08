using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class ProgressEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;

    public ProgressEndpointsTests(PomodoroApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var user = await TestUserFactory.RegisterAndLoginAsync(client, emailPrefix);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }

    [Fact]
    public async Task GetProgress_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me/progress");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProgress_UsuarioNovo_RetornaNivel1ETudoZerado()
    {
        var client = await AuthenticatedClientAsync("progressonovo");

        var response = await client.GetAsync("/api/users/me/progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgressBody>();
        body!.Level.Should().Be(1);
        body.TotalXp.Should().Be(0);
        body.Seeds.Should().Be(0);
        body.StreakDays.Should().Be(0);
    }

    [Fact]
    public async Task GetProgress_DepoisDeUmFocoConcluido_RefleteXpESementes()
    {
        var client = await AuthenticatedClientAsync("progressocomfoco");
        await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
        });

        var response = await client.GetAsync("/api/users/me/progress");
        var body = await response.Content.ReadFromJsonAsync<ProgressBody>();

        body!.TotalXp.Should().Be(25);
        body.Seeds.Should().Be(15);
    }

    [Fact]
    public async Task GetProgress_ComFusoInvalido_Retorna422()
    {
        var client = await AuthenticatedClientAsync("progressofusoinvalido");

        var response = await client.GetAsync("/api/users/me/progress?tz=Marte/Base");

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task GetProgress_IsolaProgressoEntreUsuarios()
    {
        var clientA = await AuthenticatedClientAsync("progressousera");
        var clientB = await AuthenticatedClientAsync("progressouserb");
        await clientA.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
        });

        var progressB = await (await clientB.GetAsync("/api/users/me/progress"))
            .Content.ReadFromJsonAsync<ProgressBody>();

        progressB!.TotalXp.Should().Be(0);
    }

    private sealed record ProgressBody(
        int Level, string Title, int XpInLevel, int XpForNextLevel, int TotalXp, int Seeds,
        int StreakDays, bool IsStreakAtRiskToday, int TodayFocusCount, int TodayFocusSeconds);
}
