using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Tasks.Create;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Tasks.Create;

public class CreateTaskHandlerTests
{
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly CreateTaskHandler _handler;

    public CreateTaskHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        _handler = new CreateTaskHandler(_tasks, _clock);
    }

    [Fact]
    public async Task HandleAsync_ComDadosValidos_PersisteEIgnoraUserIdDoCorpo()
    {
        var request = new CreateTaskRequest("Relatório", "Fechar o mês", "media", 4);

        var response = await _handler.HandleAsync(userId: 7, request, CancellationToken.None);

        response.Title.Should().Be("Relatório");
        response.Priority.Should().Be("media");
        response.Status.Should().Be("a_fazer");
        response.CompletedPomodoros.Should().Be(0);
        await _tasks.Received(1).AddAsync(Arg.Is<TaskItem>(t => t.UserId == 7), Arg.Any<CancellationToken>());
    }
}
