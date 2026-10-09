using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class PomodorosEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;

    public PomodorosEndpointsTests(PomodoroApiFactory factory)
    {
        _factory = factory;
    }

    private static object ValidSessionPayload(int offsetMinutes = 0) => new
    {
        type = "foco",
        status = "concluido",
        durationSeconds = 1500,
        startedAt = $"2026-01-01T{10 + offsetMinutes / 60:D2}:{offsetMinutes % 60:D2}:00Z",
        completedAt = $"2026-01-01T{10 + (offsetMinutes + 25) / 60:D2}:{(offsetMinutes + 25) % 60:D2}:00Z"
    };

    private async Task<HttpClient> AuthenticatedClientAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var user = await TestUserFactory.RegisterAndLoginAsync(client, emailPrefix);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }

    [Fact]
    public async Task CreatePomodoro_ComDadosValidos_Retorna201EApareceNoHistoricoDoDono()
    {
        var client = await AuthenticatedClientAsync("criar");

        var createResponse = await client.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/pomodoros");
        var body = await listResponse.Content.ReadAsStringAsync();
        body.Should().Contain("\"type\":\"foco\"");
    }

    [Fact]
    public async Task CreatePomodoro_ComDuracaoCustomizadaForaDaToleranciaAntiga_Retorna201()
    {
        // Antes da faixa configurável por tipo, 40min de foco (fora de 25min ± 60s) era rejeitado
        // com 422 — essa é a regressão que a US de durações configuráveis corrige (ver também
        // CreatePomodoroValidatorTests.Validate_ComDuracaoCustomizadaDentroDaFaixaDoTipo_NaoRetornaErro).
        var client = await AuthenticatedClientAsync("duracaocustom");

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 40 * 60,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:40:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePomodoro_ComTipoInvalido_Retorna422()
    {
        var client = await AuthenticatedClientAsync("invalido");

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "almoco",
            status = "concluido",
            durationSeconds = 60,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:01:00Z"
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task ListarHistorico_IsolaSessoesEntreDoisUsuarios()
    {
        var clientA = await AuthenticatedClientAsync("usera");
        var clientB = await AuthenticatedClientAsync("userb");

        await clientA.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload());
        await clientA.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload(offsetMinutes: 30));
        await clientB.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload());

        var listA = await (await clientA.GetAsync("/api/pomodoros")).Content.ReadFromJsonAsync<PagedResponse>();
        var listB = await (await clientB.GetAsync("/api/pomodoros")).Content.ReadFromJsonAsync<PagedResponse>();

        listA!.TotalCount.Should().Be(2);
        listB!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task CreatePomodoro_ComSobreposicaoDeHorarioParaOMesmoUsuario_Retorna422()
    {
        var client = await AuthenticatedClientAsync("sobreposto");
        await client.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload());

        var response = await client.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload(offsetMinutes: 10));

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task CreatePomodoro_ComMesmoHorarioParaUsuariosDiferentes_NaoConflita()
    {
        var clientA = await AuthenticatedClientAsync("semconflitoa");
        var clientB = await AuthenticatedClientAsync("semconflitob");
        await clientA.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload());

        var response = await clientB.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePomodoro_ComDataNoFuturo_Retorna422()
    {
        var client = await AuthenticatedClientAsync("futuro");
        var future = DateTime.UtcNow.AddDays(1);

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = future.ToString("O"),
            completedAt = future.AddMinutes(25).ToString("O"),
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task GetPomodoroById_DeOutroUsuario_Retorna404()
    {
        var owner = await AuthenticatedClientAsync("dono");
        var stranger = await AuthenticatedClientAsync("estranho");

        var created = await (await owner.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload()))
            .Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await stranger.GetAsync($"/api/pomodoros/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPomodoroById_DoProprioDono_Retorna200()
    {
        var owner = await AuthenticatedClientAsync("proprio");
        var created = await (await owner.PostAsJsonAsync("/api/pomodoros", ValidSessionPayload()))
            .Content.ReadFromJsonAsync<CreatedResponse>();

        var response = await owner.GetAsync($"/api/pomodoros/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record PagedResponse(int TotalCount);

    private sealed record CreatedResponse(int Id);
}
