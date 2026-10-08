using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Achievements;
using Pomodoro.Application.Achievements.Evaluate;
using Pomodoro.Application.Common;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Achievements.Evaluate;

public class EvaluateAchievementsHandlerTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly ITaskRepository _tasks = Substitute.For<ITaskRepository>();
    private readonly IAchievementRepository _achievements = Substitute.For<IAchievementRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly EvaluateAchievementsHandler _handler;
    private static readonly DateTime Now = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public EvaluateAchievementsHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _tasks.ListForUserAsync(1, TaskItemStatus.Feito, Arg.Any<CancellationToken>())
            .Returns(new List<TaskItem>());
        _achievements.ListForUserAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement>());
        _handler = new EvaluateAchievementsHandler(new AchievementStatsProvider(_sessions, _tasks), _achievements, _clock);
    }

    private static PomodoroSession BuildFocusSession(DateTime completedAt) => PomodoroSession.Create(
        1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
        completedAt.AddSeconds(-SessionTypeDurations.FocoSeconds), completedAt, completedAt);

    [Fact]
    public async Task HandleAsync_SemHistorico_NaoDesbloqueiaNadaERetornaListaVazia()
    {
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession>());

        var unlocked = await _handler.HandleAsync(1, CancellationToken.None);

        unlocked.Should().BeEmpty();
        await _achievements.DidNotReceive().AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComPrimeiroFocoConcluido_DesbloqueiaPrimeiraSemente()
    {
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession> { BuildFocusSession(Now) });

        var unlocked = await _handler.HandleAsync(1, CancellationToken.None);

        unlocked.Should().Contain("primeira_semente");
        await _achievements.Received(1).AddAsync(
            Arg.Is<UserAchievement>(a => a.UserId == 1 && a.Code == "primeira_semente" && a.UnlockedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComConquistaJaDesbloqueada_NaoGravaDeNovo()
    {
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession> { BuildFocusSession(Now) });
        _achievements.ListForUserAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { UserAchievement.Create(1, "primeira_semente", Now.AddDays(-1)) });

        var unlocked = await _handler.HandleAsync(1, CancellationToken.None);

        unlocked.Should().NotContain("primeira_semente");
        await _achievements.DidNotReceive().AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ChamadoDuasVezesSeguidas_SegundaChamadaNaoDuplica()
    {
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession> { BuildFocusSession(Now) });
        var persisted = new List<UserAchievement>();
        _achievements.ListForUserAsync(1, Arg.Any<CancellationToken>()).Returns(_ => persisted);
        _achievements.AddAsync(Arg.Any<UserAchievement>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => persisted.Add(call.Arg<UserAchievement>()));

        var firstRun = await _handler.HandleAsync(1, CancellationToken.None);
        var secondRun = await _handler.HandleAsync(1, CancellationToken.None);

        firstRun.Should().Contain("primeira_semente");
        secondRun.Should().BeEmpty();
        await _achievements.Received(1).AddAsync(
            Arg.Is<UserAchievement>(a => a.Code == "primeira_semente"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComCemFocosConcluidos_DesbloqueiaCemTomatesERetroativamentePrimeiraSemente()
    {
        var sessions = Enumerable.Range(0, 100)
            .Select(i => BuildFocusSession(Now.AddMinutes(i)))
            .ToList();
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(sessions);

        var unlocked = await _handler.HandleAsync(1, CancellationToken.None);

        unlocked.Should().Contain(new[] { "primeira_semente", "cem_tomates" });
    }
}
