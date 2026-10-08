using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common;

namespace Pomodoro.Application.Pomodoros.List;

public sealed class ListPomodorosHandler
{
    public const int DefaultLimit = 10;
    public const int MaxLimit = 100;

    private readonly IPomodoroSessionRepository _sessions;

    public ListPomodorosHandler(IPomodoroSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<PagedResult<PomodoroSessionResponse>> HandleAsync(
        int userId, int? limit, int? offset, CancellationToken cancellationToken)
    {
        var normalizedLimit = NormalizeLimit(limit);
        var normalizedOffset = offset is > 0 ? offset.Value : 0;

        var (items, totalCount) = await _sessions.ListForUserAsync(
            userId, normalizedLimit, normalizedOffset, cancellationToken);

        var responses = items.Select(PomodoroSessionResponse.FromDomain).ToList();

        return new PagedResult<PomodoroSessionResponse>(responses, totalCount, normalizedLimit, normalizedOffset);
    }

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null or <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit.Value, MaxLimit);
    }
}
