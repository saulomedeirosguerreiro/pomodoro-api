using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Tasks.Update;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.Update;

public class UpdateTaskHandlerTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly UpdateTaskHandler _handler;
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public UpdateTaskHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _handler = new UpdateTaskHandler(_tasks, _sessions, _clock);
        _sessions.CountCompletedByTaskAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());
    }

    [Fact]
    public async Task HandleAsync_ComTarefaDoProprioUsuario_AtualizaEPersiste()
    {
        var task = TaskItem.Create(7, "Rascunho", null, TaskPriority.Baixa, 2, Now);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);
        var request = new UpdateTaskRequest("Relatório final", "Revisado", "alta", 5);

        var response = await _handler.HandleAsync(1, 7, request, CancellationToken.None);

        response.Title.Should().Be("Relatório final");
        response.Priority.Should().Be("alta");
        response.EstimatedPomodoros.Should().Be(5);
        await _tasks.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTarefaInexistenteOuDeOutroUsuario_LancaNotFoundException()
    {
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);
        var request = new UpdateTaskRequest("Relatório final", null, "alta", 5);

        var act = () => _handler.HandleAsync(1, 7, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
