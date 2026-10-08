using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Tasks.SetStatus;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.SetStatus;

public class SetTaskStatusHandlerTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IAchievementEvaluator _achievementEvaluator = Substitute.For<IAchievementEvaluator>();
    private readonly SetTaskStatusHandler _handler;
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public SetTaskStatusHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _handler = new SetTaskStatusHandler(_tasks, _sessions, _clock, _achievementEvaluator);
        _sessions.CountCompletedByTaskAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());
    }

    private static TaskItem BuildTask(int userId = 7) =>
        TaskItem.Create(userId, "Relatório", null, TaskPriority.Media, 4, Now);

    [Fact]
    public async Task HandleAsync_ComTarefaInexistenteOuDeOutroUsuario_LancaNotFoundException()
    {
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var act = () => _handler.HandleAsync(1, 7, new SetTaskStatusRequest("em_curso"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_ComEmCursoSemOutraTarefaEmFoco_FocaATarefa()
    {
        var task = BuildTask();
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);
        _tasks.FindInFocusAsync(7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var response = await _handler.HandleAsync(1, 7, new SetTaskStatusRequest("em_curso"), CancellationToken.None);

        response.Status.Should().Be("em_curso");
        await _achievementEvaluator.DidNotReceive().HandleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComEmCursoEOutraTarefaJaEmFoco_DemoveAAnteriorEFocaANova()
    {
        var previouslyFocused = BuildTask();
        previouslyFocused.MarkAsFocused(Now);
        var newTask = BuildTask();
        _tasks.GetByIdForUserAsync(2, 7, Arg.Any<CancellationToken>()).Returns(newTask);
        _tasks.FindInFocusAsync(7, Arg.Any<CancellationToken>()).Returns(previouslyFocused);

        var response = await _handler.HandleAsync(2, 7, new SetTaskStatusRequest("em_curso"), CancellationToken.None);

        response.Status.Should().Be("em_curso");
        previouslyFocused.Status.Should().Be(TaskItemStatus.AFazer);
    }

    [Fact]
    public async Task HandleAsync_ComFeito_MarcaComoConcluida()
    {
        var task = BuildTask();
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);

        var response = await _handler.HandleAsync(1, 7, new SetTaskStatusRequest("feito"), CancellationToken.None);

        response.Status.Should().Be("feito");
        await _achievementEvaluator.Received(1).HandleAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComAFazer_VoltaParaAFazer()
    {
        var task = BuildTask();
        task.MarkAsFocused(Now);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);

        var response = await _handler.HandleAsync(1, 7, new SetTaskStatusRequest("a_fazer"), CancellationToken.None);

        response.Status.Should().Be("a_fazer");
    }

    [Fact]
    public async Task HandleAsync_ComTarefaJaConcluidaIndoParaEmCurso_LancaDomainException()
    {
        var task = BuildTask();
        task.MarkAsDone(Now);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);
        _tasks.FindInFocusAsync(7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var act = () => _handler.HandleAsync(1, 7, new SetTaskStatusRequest("em_curso"), CancellationToken.None);

        await act.Should().ThrowAsync<Pomodoro.Domain.Common.DomainException>();
    }
}
