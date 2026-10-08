using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Tasks.Delete;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.Delete;

public class DeleteTaskHandlerTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly DeleteTaskHandler _handler;

    public DeleteTaskHandlerTests()
    {
        _handler = new DeleteTaskHandler(_tasks);
    }

    [Fact]
    public async Task HandleAsync_ComTarefaDoProprioUsuario_Remove()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, DateTime.UtcNow);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);

        await _handler.HandleAsync(1, 7, CancellationToken.None);

        await _tasks.Received(1).DeleteAsync(task, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComTarefaInexistenteOuDeOutroUsuario_LancaNotFoundException()
    {
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var act = () => _handler.HandleAsync(1, 7, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
