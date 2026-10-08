using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Achievements;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Achievements;

public class AchievementStatsProviderTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly AchievementStatsProvider _provider;
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public AchievementStatsProviderTests()
    {
        _provider = new AchievementStatsProvider(_sessions, _tasks);
    }

    private static PomodoroSession BuildFocusSession(SessionStatus status, DateTime completedAt) =>
        PomodoroSession.Create(
            1, SessionType.Foco, status, SessionTypeDurations.FocoSeconds,
            completedAt.AddSeconds(-SessionTypeDurations.FocoSeconds), completedAt, completedAt);

    [Fact]
    public async Task BuildAsync_CombinaSessoesTarefasENivelDerivadoDoXp()
    {
        var sessions = new List<PomodoroSession>
        {
            BuildFocusSession(SessionStatus.Concluido, Now),
            BuildFocusSession(SessionStatus.Concluido, Now.AddMinutes(30)),
        };
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(sessions);
        _tasks.ListForUserAsync(1, TaskItemStatus.Feito, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem> { TaskItem.Create(1, "Feita", null, TaskPriority.Baixa, 1, Now) });

        var stats = await _provider.BuildAsync(1, CancellationToken.None);

        stats.CompletedFocusCount.Should().Be(2);
        stats.CompletedTasksCount.Should().Be(1);
        stats.Level.Should().Be(1);
    }
}
