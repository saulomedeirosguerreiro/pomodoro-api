using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Achievements;
using Pomodoro.Application.Achievements.List;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Achievements.List;

public class ListAchievementsHandlerTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IAchievementRepository _achievements = Substitute.For<IAchievementRepository>();
    private readonly IAchievementEvaluator _evaluator = Substitute.For<IAchievementEvaluator>();
    private readonly ListAchievementsHandler _handler;
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public ListAchievementsHandlerTests()
    {
        _tasks.ListForUserAsync(1, TaskItemStatus.Feito, Arg.Any<CancellationToken>()).Returns(new List<TaskItem>());
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession>());
        _achievements.ListForUserAsync(1, Arg.Any<CancellationToken>()).Returns(new List<UserAchievement>());
        _handler = new ListAchievementsHandler(_evaluator, new AchievementStatsProvider(_sessions, _tasks), _achievements);
    }

    [Fact]
    public async Task HandleAsync_SempreAvaliaRetroativamenteAntesDeResponder()
    {
        await _handler.HandleAsync(1, CancellationToken.None);

        await _evaluator.Received(1).HandleAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RetornaOCatalogoInteiroComDezItens()
    {
        var response = await _handler.HandleAsync(1, CancellationToken.None);

        response.Should().HaveCount(10);
    }

    [Fact]
    public async Task HandleAsync_ComConquistaDesbloqueada_RetornaDataDeDesbloqueioESemProgresso()
    {
        _achievements.ListForUserAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { UserAchievement.Create(1, "primeira_semente", Now) });

        var response = await _handler.HandleAsync(1, CancellationToken.None);

        var item = response.Single(a => a.Code == "primeira_semente");
        item.UnlockedAt.Should().Be(Now);
        item.ProgressCurrent.Should().BeNull();
        item.ProgressTarget.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ComConquistaBloqueadaENumerica_RetornaProgressoParcial()
    {
        var sessions = Enumerable.Range(0, 37)
            .Select(i => PomodoroSession.Create(
                1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
                Now.AddMinutes(i * 30 - SessionTypeDurations.FocoSeconds / 60), Now.AddMinutes(i * 30), Now))
            .ToList();
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(sessions);

        var response = await _handler.HandleAsync(1, CancellationToken.None);

        var item = response.Single(a => a.Code == "cem_tomates");
        item.UnlockedAt.Should().BeNull();
        item.ProgressCurrent.Should().Be(37);
        item.ProgressTarget.Should().Be(100);
    }

    [Fact]
    public async Task HandleAsync_IsolaStatusEntreUsuarios()
    {
        _achievements.ListForUserAsync(2, Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { UserAchievement.Create(2, "primeira_semente", Now) });
        _sessions.ListAllAsync(2, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession>());
        _tasks.ListForUserAsync(2, TaskItemStatus.Feito, Arg.Any<CancellationToken>()).Returns(new List<TaskItem>());

        var responseUser1 = await _handler.HandleAsync(1, CancellationToken.None);

        responseUser1.Single(a => a.Code == "primeira_semente").UnlockedAt.Should().BeNull();
    }
}
