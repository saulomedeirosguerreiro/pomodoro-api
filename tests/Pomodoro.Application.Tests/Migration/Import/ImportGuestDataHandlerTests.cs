using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Achievements;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Migration.Import;
using Pomodoro.Application.Pomodoros.Create;
using Pomodoro.Application.Tasks.Create;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Migration.Import;

public class ImportGuestDataHandlerTests
{
    private const int UserId = 7;

    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly IAchievementRepository _achievements = Substitute.For<IAchievementRepository>();
    private readonly IGuestImportRepository _imports = Substitute.For<IGuestImportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppTransaction _transaction = Substitute.For<IAppTransaction>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ImportGuestDataHandler _handler;

    private readonly List<TaskItem> _persistedTasks = [];
    private readonly List<PomodoroSession> _persistedSessions = [];
    private readonly List<UserAchievement> _persistedAchievements = [];
    private int _nextTaskId = 1;

    private static readonly DateTime Now = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    public ImportGuestDataHandlerTests()
    {
        _clock.UtcNow.Returns(Now);

        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);
        _transaction.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _transaction.RollbackAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _transaction.DisposeAsync().Returns(ValueTask.CompletedTask);

        _imports.FindAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GuestImport?)null);

        _tasks.AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call =>
            {
                var task = call.Arg<TaskItem>();
                typeof(TaskItem).GetProperty(nameof(TaskItem.Id))!.SetValue(task, _nextTaskId++);
                _persistedTasks.Add(task);
            });
        _tasks.FindInFocusAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => _persistedTasks.FirstOrDefault(
                t => t.UserId == call.ArgAt<int>(0) && t.Status == TaskItemStatus.EmCurso));
        _tasks.GetByIdForUserAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => _persistedTasks.FirstOrDefault(
                t => t.Id == call.ArgAt<int>(0) && t.UserId == call.ArgAt<int>(1)));
        _tasks.ListForUserAsync(Arg.Any<int>(), TaskItemStatus.Feito, Arg.Any<CancellationToken>())
            .Returns(call => _persistedTasks
                .Where(t => t.UserId == call.ArgAt<int>(0) && t.Status == TaskItemStatus.Feito)
                .ToList());

        _sessions.AddAsync(Arg.Any<PomodoroSession>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => _persistedSessions.Add(call.Arg<PomodoroSession>()));
        _sessions.ExistsOverlappingAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var userId = call.ArgAt<int>(0);
                var startedAt = call.ArgAt<DateTime>(1);
                var completedAt = call.ArgAt<DateTime>(2);
                return _persistedSessions.Any(
                    s => s.UserId == userId && s.StartedAt < completedAt && startedAt < s.CompletedAt);
            });
        _sessions.ListAllAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => _persistedSessions.Where(s => s.UserId == call.ArgAt<int>(0)).ToList());

        _achievements.ListForUserAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => _persistedAchievements.Where(a => a.UserId == call.ArgAt<int>(0)).ToList());
        _achievements.AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => _persistedAchievements.Add(call.Arg<UserAchievement>()));

        _handler = new ImportGuestDataHandler(
            _tasks, _sessions, _achievements, _imports, _unitOfWork, _clock,
            new AchievementStatsProvider(_sessions, _tasks),
            new CreateTaskValidator(),
            new CreatePomodoroValidator(_clock));
    }

    private static GuestTaskImportItem ValidTask(
        string localId = "task-1", string status = "a_fazer", DateTime? createdAt = null, DateTime? completedAt = null) =>
        new(localId, "Planejar a semana", null, "media", 2, status, createdAt, completedAt);

    private static GuestSessionImportItem ValidSession(
        string localId = "session-1", string? taskLocalId = null, DateTime? startedAt = null, DateTime? completedAt = null)
    {
        var completed = completedAt ?? Now.AddHours(-1);
        var started = startedAt ?? completed.AddSeconds(-SessionTypeDurations.FocoSeconds);
        return new GuestSessionImportItem(localId, "foco", "concluido", SessionTypeDurations.FocoSeconds, started, completed, taskLocalId);
    }

    [Fact]
    public async Task HandleAsync_ComLote100PorCentoValido_ImportaTudoEVinculaSessaoPorLocalId()
    {
        var request = new ImportGuestDataRequest(
            "guest-1",
            [ValidTask("task-1")],
            [ValidSession("session-1", taskLocalId: "task-1")]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.TasksImported.Should().Be(1);
        response.SessionsImported.Should().Be(1);
        response.Skipped.Should().BeEmpty();
        _persistedSessions.Should().ContainSingle(s => s.TaskItemId == _persistedTasks[0].Id);
        await _imports.Received(1).AddAsync(Arg.Any<GuestImport>(), Arg.Any<CancellationToken>());
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChamadoDuasVezesComMesmoGuestId_SegundaChamadaNaoReprocessaEDevolveRelatorioSalvo()
    {
        var achievements = new List<ImportedAchievementSummary> { new("primeira_semente", "Primeira Semente", Now) };
        var skipped = new List<ImportSkippedItem>();
        var existingRecord = GuestImport.Create(
            UserId, "guest-1", Now.AddDays(-1), 3, 4,
            JsonSerializer.Serialize(achievements), JsonSerializer.Serialize(skipped));
        _imports.FindAsync(UserId, "guest-1", Arg.Any<CancellationToken>()).Returns(existingRecord);

        var request = new ImportGuestDataRequest("guest-1", [ValidTask()], [ValidSession()]);
        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.TasksImported.Should().Be(3);
        response.SessionsImported.Should().Be(4);
        response.AchievementsUnlocked.Should().ContainSingle(a => a.Code == "primeira_semente");
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _tasks.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _sessions.DidNotReceive().AddAsync(Arg.Any<PomodoroSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComExcecaoInesperadaNoMeioDoLote_FazRollbackENaoGravaGuestImport()
    {
        _sessions.AddAsync(Arg.Any<PomodoroSession>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("falha inesperada"));
        var request = new ImportGuestDataRequest("guest-1", [ValidTask()], [ValidSession()]);

        var act = () => _handler.HandleAsync(UserId, request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _imports.DidNotReceive().AddAsync(Arg.Any<GuestImport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTarefaDeTituloVazio_PulaAItemEContinuaOResto()
    {
        var request = new ImportGuestDataRequest(
            "guest-1",
            [ValidTask("task-invalido") with { Title = "" }, ValidTask("task-valido")],
            []);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.TasksImported.Should().Be(1);
        response.Skipped.Should().ContainSingle(s => s.ItemType == "task" && s.LocalId == "task-invalido");
    }

    [Fact]
    public async Task HandleAsync_ComStatusDeTarefaInvalido_PulaAItem()
    {
        var request = new ImportGuestDataRequest(
            "guest-1", [ValidTask("task-1") with { Status = "status-invalido" }], []);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.TasksImported.Should().Be(0);
        response.Skipped.Should().ContainSingle(s => s.ItemType == "task" && s.LocalId == "task-1");
    }

    [Fact]
    public async Task HandleAsync_ComSessaoSobrepostaAOutraJaExistenteNaConta_PulaASessaoComMensagemDeSobreposicao()
    {
        var existing = PomodoroSession.Create(
            UserId, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
            Now.AddHours(-2), Now.AddHours(-2).AddSeconds(SessionTypeDurations.FocoSeconds), Now.AddHours(-2));
        _persistedSessions.Add(existing);

        var overlapping = ValidSession("session-1", startedAt: existing.StartedAt, completedAt: existing.CompletedAt);
        var request = new ImportGuestDataRequest("guest-1", [], [overlapping]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(0);
        response.Skipped.Should().ContainSingle(
            s => s.ItemType == "session" && s.Reason == "Esta sessão se sobrepõe a outra já registrada.");
    }

    [Fact]
    public async Task HandleAsync_ComDuasSessoesDoMesmoLoteSobrepostasEntreSi_PulaASegunda()
    {
        var first = ValidSession("session-1", completedAt: Now.AddHours(-2));
        var second = ValidSession("session-2", startedAt: first.StartedAt, completedAt: first.CompletedAt);
        var request = new ImportGuestDataRequest("guest-1", [], [first, second]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(1);
        response.Skipped.Should().ContainSingle(s => s.ItemType == "session" && s.LocalId == "session-2");
    }

    [Fact]
    public async Task HandleAsync_ComSessaoReferenciandoTarefaLocalPulada_PulaASessaoCitandoATarefa()
    {
        var invalidTask = ValidTask("task-invalido") with { Title = "" };
        var session = ValidSession("session-1", taskLocalId: "task-invalido");
        var request = new ImportGuestDataRequest("guest-1", [invalidTask], [session]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(0);
        response.Skipped.Should().ContainSingle(
            s => s.ItemType == "session" && s.Reason.Contains("task-invalido"));
    }

    [Fact]
    public async Task HandleAsync_ComDuasTarefasEmCursoNoMesmoLote_SoAUltimaFicaEmCurso()
    {
        var request = new ImportGuestDataRequest(
            "guest-1",
            [ValidTask("task-1", status: "em_curso"), ValidTask("task-2", status: "em_curso")],
            []);

        await _handler.HandleAsync(UserId, request, CancellationToken.None);

        _persistedTasks.Single(t => t.Id == 1).Status.Should().Be(TaskItemStatus.AFazer);
        _persistedTasks.Single(t => t.Id == 2).Status.Should().Be(TaskItemStatus.EmCurso);
    }

    [Fact]
    public async Task HandleAsync_ComTarefaEmCursoQuandoContaJaTinhaOutraEmFoco_ATarefaAntigaEDesfocada()
    {
        var alreadyFocused = TaskItem.Create(UserId, "Tarefa antiga", null, TaskPriority.Media, 1, Now.AddDays(-5));
        alreadyFocused.MarkAsFocused(Now.AddDays(-5));
        typeof(TaskItem).GetProperty(nameof(TaskItem.Id))!.SetValue(alreadyFocused, 999);
        _persistedTasks.Add(alreadyFocused);

        var request = new ImportGuestDataRequest("guest-1", [ValidTask("task-1", status: "em_curso")], []);

        await _handler.HandleAsync(UserId, request, CancellationToken.None);

        alreadyFocused.Status.Should().Be(TaskItemStatus.AFazer);
        _persistedTasks.Single(t => t.Id != 999).Status.Should().Be(TaskItemStatus.EmCurso);
    }

    [Fact]
    public async Task HandleAsync_ComTarefaFeitaComCompletedAtInformado_UsaEssaDataParaCreditarPrimeiraColheita()
    {
        var completedAt = Now.AddDays(-10);
        var request = new ImportGuestDataRequest(
            "guest-1", [ValidTask("task-1", status: "feito", createdAt: Now.AddDays(-20), completedAt: completedAt)], []);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.AchievementsUnlocked.Should().ContainSingle(a => a.Code == "primeira_colheita" && a.UnlockedAt == completedAt);
    }

    [Fact]
    public async Task HandleAsync_ComTarefaFeitaSemCompletedAtNemCreatedAt_CaiNoInstanteDaImportacao()
    {
        var request = new ImportGuestDataRequest("guest-1", [ValidTask("task-1", status: "feito")], []);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.AchievementsUnlocked.Should().ContainSingle(a => a.Code == "primeira_colheita" && a.UnlockedAt == Now);
    }

    [Fact]
    public async Task HandleAsync_ComConquistaDesbloqueadaAposSessoesHistoricas_UnlockedAtBateComOEventoHistoricoNaoComAgora()
    {
        var historicalMoment = Now.AddDays(-15);
        var session = ValidSession("session-1", completedAt: historicalMoment);
        var request = new ImportGuestDataRequest("guest-1", [], [session]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.AchievementsUnlocked.Should().ContainSingle(a => a.Code == "primeira_semente" && a.UnlockedAt == historicalMoment);
    }

    [Fact]
    public async Task HandleAsync_ComCorridaNoIndiceUnicoDeGuestImport_FazRollbackEDevolveORegistroVencedor()
    {
        var winnerAchievements = new List<ImportedAchievementSummary>();
        var winnerSkipped = new List<ImportSkippedItem>();
        var winner = GuestImport.Create(
            UserId, "guest-1", Now.AddMinutes(-1), 5, 6,
            JsonSerializer.Serialize(winnerAchievements), JsonSerializer.Serialize(winnerSkipped));

        _imports.FindAsync(UserId, "guest-1", Arg.Any<CancellationToken>()).Returns((GuestImport?)null, winner);
        _imports.AddAsync(Arg.Any<GuestImport>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DuplicateGuestImportException("corrida"));

        var request = new ImportGuestDataRequest("guest-1", [], []);
        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.TasksImported.Should().Be(5);
        response.SessionsImported.Should().Be(6);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComVinculoDeSessaoATarefaJaConcluida_PulaASessaoComMensagemGenericaDoTaskLinkGuard()
    {
        var request = new ImportGuestDataRequest(
            "guest-1",
            [ValidTask("task-1", status: "feito", createdAt: Now.AddDays(-2), completedAt: Now.AddDays(-2))],
            [ValidSession("session-1", taskLocalId: "task-1")]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.Skipped.Should().ContainSingle(
            s => s.ItemType == "session" && s.Reason == Pomodoro.Application.Pomodoros.TaskLinkGuard.GenericMessage);
    }

    [Fact]
    public async Task HandleAsync_ComConquistaJaDesbloqueadaConcorrentementeDuranteCreditoHistorico_Ignora()
    {
        _achievements.AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DuplicateUserAchievementException("corrida"));

        var request = new ImportGuestDataRequest("guest-1", [], [ValidSession()]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(1);
        response.AchievementsUnlocked.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ComConquistaJaDesbloqueadaAntesDaImportacao_NaoAparecDeNovoNoRelatorio()
    {
        _persistedAchievements.Add(UserAchievement.Create(UserId, "primeira_semente", Now.AddDays(-30)));

        var request = new ImportGuestDataRequest("guest-1", [], [ValidSession()]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.AchievementsUnlocked.Should().NotContain(a => a.Code == "primeira_semente");
        await _achievements.DidNotReceive().AddAsync(
            Arg.Is<UserAchievement>(a => a.Code == "primeira_semente"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTarefaFeitaComCompletedAtNoFuturo_SaneiaParaOInstanteDaImportacao()
    {
        var request = new ImportGuestDataRequest(
            "guest-1",
            [ValidTask("task-1", status: "feito", completedAt: Now.AddDays(100))],
            []);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.AchievementsUnlocked.Should().ContainSingle(a => a.Code == "primeira_colheita" && a.UnlockedAt == Now);
    }

    [Fact]
    public async Task HandleAsync_ComSessaoDeTipoInvalido_PulaAItem()
    {
        var invalid = ValidSession("session-1") with { Type = "tipo-invalido" };
        var request = new ImportGuestDataRequest("guest-1", [], [invalid]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(0);
        response.Skipped.Should().ContainSingle(s => s.ItemType == "session" && s.LocalId == "session-1");
    }

    [Fact]
    public async Task HandleAsync_ComSessaoComStartedAtDeKindUnspecified_UsaCompletedAtComoCreatedAt()
    {
        var completed = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);
        var started = completed.AddSeconds(-SessionTypeDurations.FocoSeconds);
        var session = new GuestSessionImportItem(
            "session-1", "foco", "concluido", SessionTypeDurations.FocoSeconds, started, completed, null);
        var request = new ImportGuestDataRequest("guest-1", [], [session]);

        var response = await _handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(1);
        _persistedSessions.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_ComValidatorDeTarefaPermissivo_TarefaInvalidaNoDominioEhPulaPeloCatchDeDefesaEmProfundidade()
    {
        var permissiveTaskValidator = Substitute.For<IValidator<CreateTaskRequest>>();
        permissiveTaskValidator
            .ValidateAsync(Arg.Any<CreateTaskRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var handler = new ImportGuestDataHandler(
            _tasks, _sessions, _achievements, _imports, _unitOfWork, _clock,
            new AchievementStatsProvider(_sessions, _tasks),
            permissiveTaskValidator,
            new CreatePomodoroValidator(_clock));

        var invalidTask = ValidTask("task-1") with { Title = "" };
        var request = new ImportGuestDataRequest("guest-1", [invalidTask], []);

        var response = await handler.HandleAsync(UserId, request, CancellationToken.None);

        response.TasksImported.Should().Be(0);
        response.Skipped.Should().ContainSingle(s => s.ItemType == "task" && s.LocalId == "task-1");
    }

    [Fact]
    public async Task HandleAsync_ComValidatorDeSessaoPermissivo_SessaoInvalidaNoDominioEhPulaPeloCatchDeDefesaEmProfundidade()
    {
        var permissiveSessionValidator = Substitute.For<IValidator<CreatePomodoroRequest>>();
        permissiveSessionValidator
            .ValidateAsync(Arg.Any<CreatePomodoroRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var handler = new ImportGuestDataHandler(
            _tasks, _sessions, _achievements, _imports, _unitOfWork, _clock,
            new AchievementStatsProvider(_sessions, _tasks),
            new CreateTaskValidator(),
            permissiveSessionValidator);

        var invalidSession = ValidSession("session-1") with { DurationSeconds = 0 };
        var request = new ImportGuestDataRequest("guest-1", [], [invalidSession]);

        var response = await handler.HandleAsync(UserId, request, CancellationToken.None);

        response.SessionsImported.Should().Be(0);
        response.Skipped.Should().ContainSingle(s => s.ItemType == "session" && s.LocalId == "session-1");
    }
}
