using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Pomodoros;
using Pomodoro.Application.Pomodoros.List;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Pomodoros.List;

public class ListPomodorosHandlerTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly ListPomodorosHandler _handler;

    public ListPomodorosHandlerTests()
    {
        _handler = new ListPomodorosHandler(_sessions);
    }

    private static readonly DateTime StartedAt = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CompletedAt = StartedAt.AddSeconds(SessionTypeDurations.FocoSeconds);
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 10, 25, 1, DateTimeKind.Utc);

    private static PomodoroSession BuildSession() => PomodoroSession.Create(
        1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
        StartedAt, CompletedAt, CreatedAt);

    [Fact]
    public async Task HandleAsync_SemLimiteOuOffset_UsaDefaults()
    {
        _sessions.ListForUserAsync(1, 10, 0, Arg.Any<CancellationToken>())
            .Returns((new List<PomodoroSession> { BuildSession() }, 1));

        var result = await _handler.HandleAsync(1, limit: null, offset: null, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new PomodoroSessionResponse(
            Id: 0,
            Type: "foco",
            Status: "concluido",
            DurationSeconds: SessionTypeDurations.FocoSeconds,
            StartedAt: StartedAt,
            CompletedAt: CompletedAt,
            CreatedAt: CreatedAt));
        result.Limit.Should().Be(10);
        result.Offset.Should().Be(0);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ComLimiteAcimaDoMaximo_ClampaEm100()
    {
        _sessions.ListForUserAsync(1, 100, 0, Arg.Any<CancellationToken>())
            .Returns((new List<PomodoroSession>(), 0));

        var result = await _handler.HandleAsync(1, limit: 500, offset: null, CancellationToken.None);

        result.Limit.Should().Be(100);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task HandleAsync_ComLimiteNaoPositivo_UsaDefault(int limit)
    {
        _sessions.ListForUserAsync(1, 10, 0, Arg.Any<CancellationToken>())
            .Returns((new List<PomodoroSession>(), 0));

        var result = await _handler.HandleAsync(1, limit, offset: null, CancellationToken.None);

        result.Limit.Should().Be(10);
    }

    [Fact]
    public async Task HandleAsync_ComOffsetPositivo_UsaOValorInformado()
    {
        _sessions.ListForUserAsync(1, 10, 5, Arg.Any<CancellationToken>())
            .Returns((new List<PomodoroSession>(), 0));

        var result = await _handler.HandleAsync(1, limit: null, offset: 5, CancellationToken.None);

        result.Offset.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_ComOffsetNegativo_UsaZero()
    {
        _sessions.ListForUserAsync(1, 10, 0, Arg.Any<CancellationToken>())
            .Returns((new List<PomodoroSession>(), 0));

        var result = await _handler.HandleAsync(1, limit: null, offset: -5, CancellationToken.None);

        result.Offset.Should().Be(0);
    }
}
