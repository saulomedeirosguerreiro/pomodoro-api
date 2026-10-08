using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Pomodoros;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Pomodoros;

public class TaskLinkGuardTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EnsureCanBeLinkedAsync_ComTarefaDoUsuarioEmAbertoESessaoDeFoco_NaoLanca()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, BaseTime);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => TaskLinkGuard.EnsureCanBeLinkedAsync(_tasks, 7, 1, SessionType.Foco, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanBeLinkedAsync_ComTarefaInexistenteOuDeOutroUsuario_LancaFieldValidationExceptionComMensagemGenerica()
    {
        _tasks.GetByIdForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var act = () => TaskLinkGuard.EnsureCanBeLinkedAsync(_tasks, 7, 99, SessionType.Foco, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<FieldValidationException>();
        exception.Which.Field.Should().Be("TaskId");
        exception.Which.Message.Should().Be(TaskLinkGuard.GenericMessage);
    }

    [Fact]
    public async Task EnsureCanBeLinkedAsync_ComTarefaJaConcluida_LancaFieldValidationException()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, BaseTime);
        task.MarkAsDone(BaseTime);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => TaskLinkGuard.EnsureCanBeLinkedAsync(_tasks, 7, 1, SessionType.Foco, CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
    }

    [Fact]
    public async Task EnsureCanBeLinkedAsync_ComSessaoQueNaoEDeFoco_LancaFieldValidationException()
    {
        var task = TaskItem.Create(7, "Relatório", null, TaskPriority.Media, 4, BaseTime);
        _tasks.GetByIdForUserAsync(1, 7, Arg.Any<CancellationToken>()).Returns(task);

        var act = () => TaskLinkGuard.EnsureCanBeLinkedAsync(
            _tasks, 7, 1, SessionType.DescansoCurto, CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>();
    }
}
