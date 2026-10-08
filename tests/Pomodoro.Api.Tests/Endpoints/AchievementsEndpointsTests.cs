using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class AchievementsEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;

    public AchievementsEndpointsTests(PomodoroApiFactory factory)
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

    private static readonly DateTime BaseStart = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static object FocusPayload(int offsetMinutes = 0)
    {
        var startedAt = BaseStart.AddMinutes(offsetMinutes);
        var completedAt = startedAt.AddMinutes(25);
        return new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = startedAt.ToString("O"),
            completedAt = completedAt.ToString("O"),
        };
    }

    [Fact]
    public async Task GetAchievements_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/achievements");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAchievements_UsuarioNovo_RetornaCatalogoInteiroTodoBloqueado()
    {
        var client = await AuthenticatedClientAsync("conquistanovo");

        var response = await client.GetAsync("/api/achievements");
        var body = await response.Content.ReadFromJsonAsync<List<AchievementBody>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().HaveCount(10);
        body!.Should().OnlyContain(a => a.UnlockedAt == null);
    }

    [Fact]
    public async Task GetAchievements_DepoisDoPrimeiroFocoConcluido_PrimeiraSementeFicaDesbloqueadaComData()
    {
        var client = await AuthenticatedClientAsync("conquistaprimeirosemente");

        await client.PostAsJsonAsync("/api/pomodoros", FocusPayload());

        var body = await (await client.GetAsync("/api/achievements")).Content.ReadFromJsonAsync<List<AchievementBody>>();

        var item = body!.Single(a => a.Code == "primeira_semente");
        item.UnlockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAchievements_ConsultadoDuasVezes_NaoDuplicaAConquistaJaDesbloqueada()
    {
        var client = await AuthenticatedClientAsync("conquistaidempotente");
        await client.PostAsJsonAsync("/api/pomodoros", FocusPayload());

        var first = await (await client.GetAsync("/api/achievements")).Content.ReadFromJsonAsync<List<AchievementBody>>();
        var second = await (await client.GetAsync("/api/achievements")).Content.ReadFromJsonAsync<List<AchievementBody>>();

        first!.Should().HaveCount(10);
        second!.Should().HaveCount(10);
        first.Single(a => a.Code == "primeira_semente").UnlockedAt
            .Should().Be(second.Single(a => a.Code == "primeira_semente").UnlockedAt);
    }

    [Fact]
    public async Task GetAchievements_ComCemFocosConcluidos_CemTomatesApareceDesbloqueadaNaPrimeiraConsulta()
    {
        var client = await AuthenticatedClientAsync("conquistacemtomates");
        for (var i = 0; i < 100; i++)
        {
            await client.PostAsJsonAsync("/api/pomodoros", FocusPayload(offsetMinutes: i * 30));
        }

        var body = await (await client.GetAsync("/api/achievements")).Content.ReadFromJsonAsync<List<AchievementBody>>();

        body!.Single(a => a.Code == "cem_tomates").UnlockedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAchievements_ComMenosDeCemFocos_CemTomatesMostraProgressoParcial()
    {
        var client = await AuthenticatedClientAsync("conquistaprogresso");
        for (var i = 0; i < 37; i++)
        {
            await client.PostAsJsonAsync("/api/pomodoros", FocusPayload(offsetMinutes: i * 30));
        }

        var body = await (await client.GetAsync("/api/achievements")).Content.ReadFromJsonAsync<List<AchievementBody>>();

        var item = body!.Single(a => a.Code == "cem_tomates");
        item.UnlockedAt.Should().BeNull();
        item.ProgressCurrent.Should().Be(37);
        item.ProgressTarget.Should().Be(100);
    }

    [Fact]
    public async Task GetAchievements_IsolaStatusEntreDoisUsuarios()
    {
        var clientA = await AuthenticatedClientAsync("conquistausera");
        var clientB = await AuthenticatedClientAsync("conquistauserb");
        await clientA.PostAsJsonAsync("/api/pomodoros", FocusPayload());

        var bodyB = await (await clientB.GetAsync("/api/achievements")).Content.ReadFromJsonAsync<List<AchievementBody>>();

        bodyB!.Single(a => a.Code == "primeira_semente").UnlockedAt.Should().BeNull();
    }

    private sealed record AchievementBody(
        string Code, string Name, string Description, DateTime? UnlockedAt, int? ProgressCurrent, int? ProgressTarget);
}
