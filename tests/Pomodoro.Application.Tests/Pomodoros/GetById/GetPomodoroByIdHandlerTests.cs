using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Pomodoros;
using Pomodoro.Application.Pomodoros.GetById;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Pomodoros.GetById;

public class GetPomodoroByIdHandlerTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly GetPomodoroByIdHandler _handler;

    public GetPomodoroByIdHandlerTests()
    {
        _handler = new GetPomodoroByIdHandler(_sessions);
    }

    [Fact]
    public async Task HandleAsync_ComSessaoDoProprioUsuario_RetornaSessao()
    {
        var startedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var completedAt = startedAt.AddSeconds(SessionTypeDurations.FocoSeconds);
        var createdAt = new DateTime(2026, 1, 1, 10, 25, 1, DateTimeKind.Utc);
        var session = PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
            startedAt, completedAt, createdAt);

        _sessions.GetByIdForUserAsync(10, 1, Arg.Any<CancellationToken>()).Returns(session);

        var response = await _handler.HandleAsync(10, 1, CancellationToken.None);

        response.Should().BeEquivalentTo(new PomodoroSessionResponse(
            Id: 0,
            Type: "foco",
            Status: "concluido",
            DurationSeconds: SessionTypeDurations.FocoSeconds,
            StartedAt: startedAt,
            CompletedAt: completedAt,
            CreatedAt: createdAt));
    }

    [Fact]
    public async Task HandleAsync_ComSessaoInexistenteOuDeOutroUsuario_LancaNotFoundException()
    {
        _sessions.GetByIdForUserAsync(10, 1, Arg.Any<CancellationToken>()).Returns((PomodoroSession?)null);

        var act = () => _handler.HandleAsync(10, 1, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
