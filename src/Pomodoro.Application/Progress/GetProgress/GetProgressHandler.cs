using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Enums;
using Pomodoro.Domain.Services;

namespace Pomodoro.Application.Progress.GetProgress;

public sealed class GetProgressHandler
{
    private const string DefaultTimeZoneId = "America/Sao_Paulo";

    private readonly IPomodoroSessionRepository _sessions;
    private readonly IDateTimeProvider _clock;

    public GetProgressHandler(IPomodoroSessionRepository sessions, IDateTimeProvider clock)
    {
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<ProgressResponse> HandleAsync(int userId, string? tz, CancellationToken cancellationToken)
    {
        var timeZone = ResolveTimeZone(tz);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, timeZone));

        var sessions = await _sessions.ListAllAsync(userId, cancellationToken);

        var totalXp = 0;
        var seeds = 0;
        var activeFocusDates = new HashSet<DateOnly>();
        var todayFocusCount = 0;
        var todayFocusSeconds = 0;

        foreach (var session in sessions)
        {
            totalXp += ProgressRules.XpFor(session.Type, session.Status);
            seeds += ProgressRules.SeedsFor(session.Type, session.Status);

            if (session.Type != SessionType.Foco || session.Status != SessionStatus.Concluido)
            {
                continue;
            }

            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(session.CompletedAt, timeZone));
            activeFocusDates.Add(localDate);

            if (localDate == today)
            {
                todayFocusCount++;
                todayFocusSeconds += session.DurationSeconds;
            }
        }

        var (level, xpInLevel, xpForNextLevel) = ProgressRules.CalculateLevel(totalXp);
        var streakDays = ProgressRules.CalculateStreakDays(activeFocusDates, today);

        return new ProgressResponse(
            Level: level,
            Title: ProgressRules.TitleFor(level),
            XpInLevel: xpInLevel,
            XpForNextLevel: xpForNextLevel,
            TotalXp: totalXp,
            Seeds: seeds,
            StreakDays: streakDays,
            IsStreakAtRiskToday: !activeFocusDates.Contains(today),
            TodayFocusCount: todayFocusCount,
            TodayFocusSeconds: todayFocusSeconds);
    }

    private static TimeZoneInfo ResolveTimeZone(string? tz)
    {
        var timeZoneId = string.IsNullOrWhiteSpace(tz) ? DefaultTimeZoneId : tz;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new FieldValidationException("tz", "Fuso horário inválido.");
        }
    }
}
