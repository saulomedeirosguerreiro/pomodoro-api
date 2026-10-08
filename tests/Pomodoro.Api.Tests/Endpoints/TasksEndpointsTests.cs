using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Pomodoro.Api.Tests.Infrastructure;
using Xunit;

namespace Pomodoro.Api.Tests.Endpoints;

public class TasksEndpointsTests : IClassFixture<PomodoroApiFactory>
{
    private readonly PomodoroApiFactory _factory;

    public TasksEndpointsTests(PomodoroApiFactory factory)
    {
        _factory = factory;
    }

    private static object ValidTaskPayload(string title = "Relatório", int estimatedPomodoros = 4) => new
    {
        title,
        description = "Fechar o relatório do mês",
        priority = "media",
        estimatedPomodoros,
    };

    private async Task<HttpClient> AuthenticatedClientAsync(string emailPrefix)
    {
        var client = _factory.CreateClient();
        var user = await TestUserFactory.RegisterAndLoginAsync(client, emailPrefix);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        return client;
    }

    [Fact]
    public async Task CreateTask_ComDadosValidos_Retorna201EApareceNaListagemDoDono()
    {
        var client = await AuthenticatedClientAsync("criartarefa");

        var createResponse = await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var tasks = await (await client.GetAsync("/api/tasks")).Content.ReadFromJsonAsync<List<TaskBody>>();
        tasks.Should().ContainSingle(t => t.Title == "Relatório" && t.Status == "a_fazer");
    }

    [Fact]
    public async Task CreateTask_ComTituloVazio_Retorna422()
    {
        var client = await AuthenticatedClientAsync("tarefainvalida");

        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "",
            description = (string?)null,
            priority = "media",
            estimatedPomodoros = 4,
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task ListTasks_IsolaTarefasEntreDoisUsuarios()
    {
        var clientA = await AuthenticatedClientAsync("tarefasusera");
        var clientB = await AuthenticatedClientAsync("tarefasuserb");
        await clientA.PostAsJsonAsync("/api/tasks", ValidTaskPayload("Tarefa de A"));
        await clientB.PostAsJsonAsync("/api/tasks", ValidTaskPayload("Tarefa de B"));

        var tasksA = await (await clientA.GetAsync("/api/tasks")).Content.ReadFromJsonAsync<List<TaskBody>>();

        tasksA.Should().ContainSingle(t => t.Title == "Tarefa de A");
    }

    [Fact]
    public async Task ListTasks_ComFiltroDeStatus_RetornaApenasOStatusPedido()
    {
        var client = await AuthenticatedClientAsync("tarefasfiltro");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload("A concluir")))
            .Content.ReadFromJsonAsync<TaskBody>();
        await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload("Pendente"));
        await client.PatchAsJsonAsync($"/api/tasks/{created!.Id}/status", new { status = "feito" });

        var tasks = await (await client.GetAsync("/api/tasks?status=feito")).Content.ReadFromJsonAsync<List<TaskBody>>();

        tasks.Should().ContainSingle(t => t.Title == "A concluir");
    }

    [Fact]
    public async Task ListTasks_ComStatusInvalido_Retorna422()
    {
        var client = await AuthenticatedClientAsync("tarefasstatusinvalido");

        var response = await client.GetAsync("/api/tasks?status=cancelada");

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task UpdateTask_ComDonoCorreto_AtualizaCampos()
    {
        var client = await AuthenticatedClientAsync("tarefaupdate");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await client.PatchAsJsonAsync($"/api/tasks/{created!.Id}", new
        {
            title = "Relatório revisado",
            description = (string?)null,
            priority = "alta",
            estimatedPomodoros = 6,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TaskBody>();
        body!.Title.Should().Be("Relatório revisado");
        body.Priority.Should().Be("alta");
    }

    [Fact]
    public async Task UpdateTask_DeOutroUsuario_Retorna404()
    {
        var owner = await AuthenticatedClientAsync("tarefadono");
        var stranger = await AuthenticatedClientAsync("tarefaestranho");
        var created = await (await owner.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await stranger.PatchAsJsonAsync($"/api/tasks/{created!.Id}", ValidTaskPayload("Hackeado"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetTaskStatus_ParaEmCursoComOutraTarefaJaEmFoco_DemoveAAnteriorEFocaANova()
    {
        var client = await AuthenticatedClientAsync("tarefafoco");
        var first = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload("Primeira")))
            .Content.ReadFromJsonAsync<TaskBody>();
        var second = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload("Segunda")))
            .Content.ReadFromJsonAsync<TaskBody>();
        await client.PatchAsJsonAsync($"/api/tasks/{first!.Id}/status", new { status = "em_curso" });

        var response = await client.PatchAsJsonAsync($"/api/tasks/{second!.Id}/status", new { status = "em_curso" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await (await client.GetAsync("/api/tasks")).Content.ReadFromJsonAsync<List<TaskBody>>();
        tasks!.Single(t => t.Id == first.Id).Status.Should().Be("a_fazer");
        tasks!.Single(t => t.Id == second.Id).Status.Should().Be("em_curso");
    }

    [Fact]
    public async Task SetTaskStatus_ComStatusInvalido_Retorna422()
    {
        var client = await AuthenticatedClientAsync("tarefastatusinvalido");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await client.PatchAsJsonAsync($"/api/tasks/{created!.Id}/status", new { status = "pausado" });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task DeleteTask_ComDonoCorreto_RemoveENaoAfetaPomodorosJaRegistrados()
    {
        var client = await AuthenticatedClientAsync("tarefadelete");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();
        await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
            taskId = created!.Id,
        });

        var deleteResponse = await client.DeleteAsync($"/api/tasks/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var pomodoros = await client.GetAsync("/api/pomodoros");
        pomodoros.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await pomodoros.Content.ReadFromJsonAsync<PagedResponse>();
        body!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteTask_DeOutroUsuario_Retorna404()
    {
        var owner = await AuthenticatedClientAsync("tarefadeletedono");
        var stranger = await AuthenticatedClientAsync("tarefadeleteestranho");
        var created = await (await owner.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await stranger.DeleteAsync($"/api/tasks/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePomodoro_SemTaskId_ContinuaIdenticoAoMvp()
    {
        var client = await AuthenticatedClientAsync("pomodorosemtask");

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePomodoro_ComTaskIdDoUsuarioEFoco_IncrementaCompletedPomodorosDaTarefa()
    {
        var client = await AuthenticatedClientAsync("pomodorocomtask");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
            taskId = created!.Id,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tasks = await (await client.GetAsync("/api/tasks")).Content.ReadFromJsonAsync<List<TaskBody>>();
        tasks!.Single(t => t.Id == created.Id).CompletedPomodoros.Should().Be(1);
    }

    [Fact]
    public async Task CreatePomodoro_ComTaskIdDeOutroUsuario_Retorna422ENaoRevelaExistencia()
    {
        var owner = await AuthenticatedClientAsync("pomodorotaskdono");
        var stranger = await AuthenticatedClientAsync("pomodorotaskestranho");
        var created = await (await owner.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await stranger.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
            taskId = created!.Id,
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task CreatePomodoro_ComTaskIdEmSessaoDeDescanso_Retorna422()
    {
        var client = await AuthenticatedClientAsync("pomodorotaskdescanso");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "descanso_curto",
            status = "concluido",
            durationSeconds = 300,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:05:00Z",
            taskId = created!.Id,
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task CreatePomodoro_ComTaskIdJaConcluida_Retorna422()
    {
        var client = await AuthenticatedClientAsync("pomodorotaskconcluida");
        var created = await (await client.PostAsJsonAsync("/api/tasks", ValidTaskPayload()))
            .Content.ReadFromJsonAsync<TaskBody>();
        await client.PatchAsJsonAsync($"/api/tasks/{created!.Id}/status", new { status = "feito" });

        var response = await client.PostAsJsonAsync("/api/pomodoros", new
        {
            type = "foco",
            status = "concluido",
            durationSeconds = 1500,
            startedAt = "2026-01-01T10:00:00Z",
            completedAt = "2026-01-01T10:25:00Z",
            taskId = created.Id,
        });

        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    private sealed record TaskBody(
        int Id, string Title, string? Description, string Priority, int EstimatedPomodoros,
        int CompletedPomodoros, string Status, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record PagedResponse(int TotalCount);
}
