using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Pomodoros;
using Pomodoro.Application.Pomodoros.Create;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Pomodoros.Create;

public class CreatePomodoroHandlerTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IAchievementEvaluator _achievementEvaluator = Substitute.For<IAchievementEvaluator>();
    private readonly CreatePomodoroHandler _handler;
    private static readonly DateTime StartedAt = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CompletedAt = StartedAt.AddSeconds(SessionTypeDurations.FocoSeconds);

    public CreatePomodoroHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 1, 1, 10, 25, 0, DateTimeKind.Utc));
        _handler = new CreatePomodoroHandler(_sessions, _tasks, _clock, _achievementEvaluator);
    }

    [Fact]
    public async Task HandleAsync_ComDadosValidos_PersisteEIgnoraUserIdDoCorpo()
    {
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt);

        var response = await _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        response.Should().BeEquivalentTo(new PomodoroSessionResponse(
            Id: 0,
            Type: "foco",
            Status: "concluido",
            DurationSeconds: SessionTypeDurations.FocoSeconds,
            StartedAt: StartedAt,
            CompletedAt: CompletedAt,
            CreatedAt: _clock.UtcNow));
        await _sessions.Received(1).AddAsync(
            Arg.Is<PomodoroSession>(s => s.UserId == 7), Arg.Any<CancellationToken>());
        await _achievementEvaluator.Received(1).HandleAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComModeFlexivelEMetadados_RepassaOsCamposParaAResposta()
    {
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt,
            Mode: PomodoroSessionMode.Flexivel, PlannedDurationSeconds: 20 * 60, AddedSeconds: 5 * 60);

        var response = await _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        response.Mode.Should().Be(PomodoroSessionMode.Flexivel);
        response.PlannedDurationSeconds.Should().Be(20 * 60);
        response.AddedSeconds.Should().Be(5 * 60);
    }

    [Fact]
    public async Task HandleAsync_ComSessaoSobreposta_LancaFieldValidationExceptionENaoPersiste()
    {
        _sessions.ExistsOverlappingAsync(7, StartedAt, CompletedAt, Arg.Any<CancellationToken>()).Returns(true);
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt);

        var act = () => _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
        await _sessions.DidNotReceive().AddAsync(Arg.Any<PomodoroSession>(), Arg.Any<CancellationToken>());
        await _achievementEvaluator.DidNotReceive().HandleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTaskIdDoUsuarioEFoco_VinculaATarefa()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, _clock.UtcNow);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt, TaskId: 1);

        var response = await _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        response.Should().NotBeNull();
        await _sessions.Received(1).AddAsync(
            Arg.Is<PomodoroSession>(s => s.TaskItemId == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTaskIdInexistenteOuDeOutroUsuario_LancaFieldValidationException()
    {
        _tasks.GetByIdForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt, TaskId: 99);

        var act = () => _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
        await _sessions.DidNotReceive().AddAsync(Arg.Any<PomodoroSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTaskIdJaConcluida_LancaFieldValidationException()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, _clock.UtcNow);
        task.MarkAsDone(_clock.UtcNow);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);
        var request = new CreatePomodoroRequest(
            "foco", "concluido", SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt, TaskId: 1);

        var act = () => _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
    }

    [Fact]
    public async Task HandleAsync_ComTaskIdEmSessaoDeDescanso_LancaFieldValidationException()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, _clock.UtcNow);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);
        var request = new CreatePomodoroRequest(
            "descanso_curto", "concluido", SessionTypeDurations.DescansoCurtoSeconds,
            StartedAt, StartedAt.AddSeconds(SessionTypeDurations.DescansoCurtoSeconds), TaskId: 1);

        var act = () => _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
    }
}
