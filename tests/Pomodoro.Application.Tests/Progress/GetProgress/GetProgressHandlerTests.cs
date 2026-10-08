using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Application.Progress.GetProgress;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Xunit;

namespace Pomodoro.Application.Tests.Progress.GetProgress;

public class GetProgressHandlerTests
{
    private readonly IPomodoroSessionRepository _sessions = Substitute.For<IPomodoroSessionRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly GetProgressHandler _handler;

    public GetProgressHandlerTests()
    {
        _handler = new GetProgressHandler(_sessions, _clock);
    }

    private static PomodoroSession Session(
        SessionType type, SessionStatus status, DateTime completedAt, int durationSeconds) =>
        PomodoroSession.Create(
            1, type, status, durationSeconds, completedAt.AddSeconds(-durationSeconds), completedAt, completedAt);

    private void GivenSessions(params PomodoroSession[] sessions) =>
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(sessions);

    [Fact]
    public async Task HandleAsync_CalculaXpESementesSobreOHistoricoCompleto()
    {
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(now);
        GivenSessions(
            Session(SessionType.Foco, SessionStatus.Concluido, now.AddHours(-1), 1500),
            Session(SessionType.Foco, SessionStatus.Concluido, now.AddHours(-2), 1500),
            Session(SessionType.Foco, SessionStatus.Concluido, now.AddHours(-3), 1500),
            Session(SessionType.Foco, SessionStatus.Interrompido, now.AddHours(-4), 600),
            Session(SessionType.DescansoCurto, SessionStatus.Concluido, now.AddHours(-5), 300),
            Session(SessionType.DescansoCurto, SessionStatus.Concluido, now.AddHours(-6), 300));

        var response = await _handler.HandleAsync(1, tz: "UTC", CancellationToken.None);

        response.TotalXp.Should().Be(85);
        response.Seeds.Should().Be(45);
    }

    [Fact]
    public async Task HandleAsync_Com1020XpTotais_RetornaNivel3ETitulo()
    {
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(now);
        // 1020 XP = 40 focos concluídos de 25 XP (1000) + 4 descansos concluídos de 5 XP (20), sem sobrepor.
        var sessions = new List<PomodoroSession>();
        for (var i = 0; i < 40; i++)
        {
            sessions.Add(Session(SessionType.Foco, SessionStatus.Concluido, now.AddHours(-1 - i), 1500));
        }
        for (var i = 0; i < 4; i++)
        {
            sessions.Add(Session(SessionType.DescansoCurto, SessionStatus.Concluido, now.AddHours(-50 - i), 300));
        }
        GivenSessions(sessions.ToArray());

        var response = await _handler.HandleAsync(1, tz: "UTC", CancellationToken.None);

        response.TotalXp.Should().Be(1020);
        response.Level.Should().Be(3);
        response.XpInLevel.Should().Be(420);
        response.XpForNextLevel.Should().Be(600);
        response.Title.Should().Be("Jardineiro Produtivo");
    }

    [Fact]
    public async Task HandleAsync_FocoConcluidoTardeDaNoiteEmSaoPaulo_ContaParaODiaLocalCorreto()
    {
        // 23:30 de 07/10 em America/Sao_Paulo (UTC-3, sem horário de verão) = 02:30 UTC de 08/10.
        var completedAtUtc = new DateTime(2026, 10, 8, 2, 30, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(completedAtUtc);
        GivenSessions(Session(SessionType.Foco, SessionStatus.Concluido, completedAtUtc, 1500));

        var response = await _handler.HandleAsync(1, tz: "America/Sao_Paulo", CancellationToken.None);

        // "Agora" (completedAtUtc) também é 23:30 local de 07/10 — mesmo dia da sessão.
        response.TodayFocusCount.Should().Be(1);
        response.TodayFocusSeconds.Should().Be(1500);
        response.StreakDays.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ComFocosOntemEAnteontemSemHoje_RetornaStreak2EEmRisco()
    {
        var now = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(now);
        GivenSessions(
            Session(SessionType.Foco, SessionStatus.Concluido, now.AddDays(-1), 1500),
            Session(SessionType.Foco, SessionStatus.Concluido, now.AddDays(-2), 1500));

        var response = await _handler.HandleAsync(1, tz: "UTC", CancellationToken.None);

        response.StreakDays.Should().Be(2);
        response.IsStreakAtRiskToday.Should().BeTrue();
        response.TodayFocusCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ComFusoInvalido_LancaFieldValidationException()
    {
        _clock.UtcNow.Returns(DateTime.UtcNow);
        GivenSessions();

        var act = () => _handler.HandleAsync(1, tz: "Marte/Base", CancellationToken.None);

        await act.Should().ThrowAsync<FieldValidationException>()
            .Where(e => e.Field == "tz");
    }

    [Fact]
    public async Task HandleAsync_SemTz_UsaSaoPauloComoDefault()
    {
        _clock.UtcNow.Returns(DateTime.UtcNow);
        GivenSessions();

        var act = () => _handler.HandleAsync(1, tz: null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_IsolaSessoesPorUsuario()
    {
        _sessions.ListAllAsync(1, Arg.Any<CancellationToken>()).Returns(new List<PomodoroSession>());
        _clock.UtcNow.Returns(DateTime.UtcNow);

        var response = await _handler.HandleAsync(1, tz: "UTC", CancellationToken.None);

        await _sessions.Received(1).ListAllAsync(1, Arg.Any<CancellationToken>());
        response.TotalXp.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_UsuarioNovoSemSessoes_RetornaValoresZerados()
    {
        _clock.UtcNow.Returns(DateTime.UtcNow);
        GivenSessions();

        var response = await _handler.HandleAsync(1, tz: "UTC", CancellationToken.None);

        response.Level.Should().Be(1);
        response.TotalXp.Should().Be(0);
        response.Seeds.Should().Be(0);
        response.StreakDays.Should().Be(0);
    }
}
