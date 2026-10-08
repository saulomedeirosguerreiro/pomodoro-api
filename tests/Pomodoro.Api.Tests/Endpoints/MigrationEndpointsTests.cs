using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class MigrationEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;

    public MigrationEndpointsTests(PomodoroApiFactory factory)
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

    private static object ValidBatch(string guestId) => new
    {
        guestId,
        tasks = new[]
        {
            new
            {
                localId = "task-1",
                title = "Planejar a semana",
                description = (string?)null,
                priority = "media",
                estimatedPomodoros = 2,
                status = "a_fazer",
                createdAt = (DateTime?)null,
                completedAt = (DateTime?)null,
            },
        },
        sessions = new[]
        {
            new
            {
                localId = "session-1",
                type = "foco",
                status = "concluido",
                durationSeconds = 1500,
                startedAt = DateTime.UtcNow.AddHours(-2).AddSeconds(-1500),
                completedAt = DateTime.UtcNow.AddHours(-2),
                taskLocalId = (string?)null,
            },
        },
    };

    [Fact]
    public async Task ImportarLote_ComDadosValidos_Retorna200EApareceEmTasksEPomodoros()
    {
        var client = await AuthenticatedClientAsync("import-valido");

        var response = await client.PostAsJsonAsync("/api/migration/import", ValidBatch($"guest-{Guid.NewGuid():N}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasksBody = await (await client.GetAsync("/api/tasks")).Content.ReadAsStringAsync();
        tasksBody.Should().Contain("Planejar a semana");

        var pomodorosBody = await (await client.GetAsync("/api/pomodoros")).Content.ReadAsStringAsync();
        pomodorosBody.Should().Contain("\"type\":\"foco\"");
    }

    [Fact]
    public async Task ImportarLote_ReenviadoComMesmoGuestId_Retorna200ComCorpoIdenticoSemDuplicarPomodoros()
    {
        var client = await AuthenticatedClientAsync("import-replay");
        var guestId = $"guest-{Guid.NewGuid():N}";

        var firstResponse = await client.PostAsJsonAsync("/api/migration/import", ValidBatch(guestId));
        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        var secondResponse = await client.PostAsJsonAsync("/api/migration/import", ValidBatch(guestId));
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondBody.Should().Be(firstBody);

        var pomodorosBody = await (await client.GetAsync("/api/pomodoros")).Content.ReadFromJsonAsync<PagedResponse>();
        pomodorosBody!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportarLote_ParaUmUsuario_NaoApareceParaOutroUsuario()
    {
        var clientA = await AuthenticatedClientAsync("import-usera");
        var clientB = await AuthenticatedClientAsync("import-userb");

        await clientA.PostAsJsonAsync("/api/migration/import", ValidBatch($"guest-{Guid.NewGuid():N}"));

        var tasksB = await (await clientB.GetAsync("/api/tasks")).Content.ReadAsStringAsync();
        tasksB.Should().NotContain("Planejar a semana");
    }

    [Fact]
    public async Task ImportarLote_ComItemInvalidoNoMeioDoLote_Retorna200ComSkippedPreenchidoEOResto()
    {
        var client = await AuthenticatedClientAsync("import-skip");

        var payload = new
        {
            guestId = $"guest-{Guid.NewGuid():N}",
            tasks = new[]
            {
                new
                {
                    localId = "task-invalido",
                    title = "",
                    description = (string?)null,
                    priority = "media",
                    estimatedPomodoros = 2,
                    status = "a_fazer",
                    createdAt = (DateTime?)null,
                    completedAt = (DateTime?)null,
                },
                new
                {
                    localId = "task-valido",
                    title = "Tarefa válida",
                    description = (string?)null,
                    priority = "media",
                    estimatedPomodoros = 2,
                    status = "a_fazer",
                    createdAt = (DateTime?)null,
                    completedAt = (DateTime?)null,
                },
            },
            sessions = Array.Empty<object>(),
        };

        var response = await client.PostAsJsonAsync("/api/migration/import", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("task-invalido");

        var tasksBody = await (await client.GetAsync("/api/tasks")).Content.ReadAsStringAsync();
        tasksBody.Should().Contain("Tarefa válida");
    }

    [Fact]
    public async Task ImportarLote_SemAutenticacao_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/migration/import", ValidBatch("guest-sem-auth"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportarLote_ComGuestIdVazio_Retorna422()
    {
        var client = await AuthenticatedClientAsync("import-guestid-vazio");

        var response = await client.PostAsJsonAsync("/api/migration/import", new
        {
            guestId = "",
            tasks = Array.Empty<object>(),
            sessions = Array.Empty<object>(),
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    private sealed record PagedResponse(int TotalCount);
}
