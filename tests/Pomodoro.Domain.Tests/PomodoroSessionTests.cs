using FluentAssertions;
using Pomodoro.Domain.Common;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Domain.Tests;

public class PomodoroSessionTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartedAt = new(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CompletedAt = StartedAt.AddSeconds(SessionTypeDurations.FocoSeconds);

    [Fact]
    public void Create_ComDadosValidos_PreencheTodosOsCampos()
    {
        var session = PomodoroSession.Create(
            userId: 1,
            type: SessionType.Foco,
            status: SessionStatus.Concluido,
            durationSeconds: SessionTypeDurations.FocoSeconds,
            startedAt: StartedAt,
            completedAt: CompletedAt,
            utcNow: UtcNow);

        session.UserId.Should().Be(1);
        session.Type.Should().Be(SessionType.Foco);
        session.Status.Should().Be(SessionStatus.Concluido);
        session.DurationSeconds.Should().Be(SessionTypeDurations.FocoSeconds);
        session.StartedAt.Should().Be(StartedAt);
        session.CompletedAt.Should().Be(CompletedAt);
        session.CreatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_ComDuracaoDentroDaTolerancia_NaoLanca()
    {
        var act = () => PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Concluido,
            SessionTypeDurations.FocoSeconds + SessionTypeDurations.ToleranceSeconds,
            StartedAt, CompletedAt, UtcNow);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ComDuracaoNaoPositiva_LancaDomainException(int duration)
    {
        var act = () => PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Interrompido, duration, StartedAt, CompletedAt, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ComDuracaoAcimaDoMaximoPermitido_LancaDomainException()
    {
        var act = () => PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Concluido,
            SessionTypeDurations.FocoSeconds + SessionTypeDurations.ToleranceSeconds + 1,
            StartedAt, CompletedAt, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ComUserIdInvalido_LancaDomainException()
    {
        var act = () => PomodoroSession.Create(
            0, SessionType.Foco, SessionStatus.Concluido,
            SessionTypeDurations.FocoSeconds, StartedAt, CompletedAt, UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_ComCompletedAtAnteriorAoStartedAt_LancaDomainException()
    {
        var act = () => PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Interrompido,
            60, StartedAt, StartedAt.AddSeconds(-1), UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_FocoComTaskItemId_VinculaATarefa()
    {
        var session = PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
            StartedAt, CompletedAt, UtcNow, taskItemId: 42);

        session.TaskItemId.Should().Be(42);
    }

    [Fact]
    public void Create_SemTaskItemId_TaskItemIdFicaNulo()
    {
        var session = PomodoroSession.Create(
            1, SessionType.Foco, SessionStatus.Concluido, SessionTypeDurations.FocoSeconds,
            StartedAt, CompletedAt, UtcNow);

        session.TaskItemId.Should().BeNull();
    }

    [Theory]
    [InlineData(SessionType.DescansoCurto)]
    [InlineData(SessionType.DescansoLongo)]
    public void Create_DescansoComTaskItemId_LancaDomainException(SessionType type)
    {
        var act = () => PomodoroSession.Create(
            1, type, SessionStatus.Concluido, SessionTypeDurations.StandardSecondsFor(type),
            StartedAt, StartedAt.AddSeconds(SessionTypeDurations.StandardSecondsFor(type)), UtcNow, taskItemId: 1);

        act.Should().Throw<DomainException>();
    }
}
