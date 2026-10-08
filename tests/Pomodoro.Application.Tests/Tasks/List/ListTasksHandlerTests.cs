using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Tasks.List;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.List;

public class ListTasksHandlerTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly ListTasksHandler _handler;
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public ListTasksHandlerTests()
    {
        _handler = new ListTasksHandler(_tasks, _sessions);
    }

    private static TaskItem BuildTask(int userId = 1) =>
        TaskItem.Create(userId, "Relatório", null, TaskPriority.Media, 4, Now);

    [Fact]
    public async Task HandleAsync_SemFiltroDeStatus_ListaTodasEIncluiContagemDePomodoros()
    {
        var task = BuildTask();
        _tasks.ListForUserAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
        _sessions.CountCompletedByTaskAsync(Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(task.Id)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [task.Id] = 3 });

        var result = await _handler.HandleAsync(1, status: null, CancellationToken.None);

        result.Should().ContainSingle().Which.CompletedPomodoros.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_ComFiltroDeStatusValido_RepassaStatusParseadoAoRepositorio()
    {
        _tasks.ListForUserAsync(1, TaskItemStatus.Feito, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _sessions.CountCompletedByTaskAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());

        var result = await _handler.HandleAsync(1, status: "feito", CancellationToken.None);

        result.Should().BeEmpty();
        await _tasks.Received(1).ListForUserAsync(1, TaskItemStatus.Feito, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComFiltroDeStatusInvalido_LancaFieldValidationException()
    {
        var act = () => _handler.HandleAsync(1, status: "cancelada", CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
    }

    [Fact]
    public async Task HandleAsync_ComTarefaSemPomodorosConcluidos_RetornaZero()
    {
        var task = BuildTask();
        _tasks.ListForUserAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { task });
        _sessions.CountCompletedByTaskAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());

        var result = await _handler.HandleAsync(1, status: null, CancellationToken.None);

        result.Should().ContainSingle().Which.CompletedPomodoros.Should().Be(0);
    }
}
