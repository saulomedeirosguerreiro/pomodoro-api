using Microsoft.EntityFrameworkCore;
using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Repositories;

public sealed class PomodoroSessionRepository : IPomodoroSessionRepository
{
    private readonly PomodoroDbContext _db;

    public PomodoroSessionRepository(PomodoroDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PomodoroSession session, CancellationToken cancellationToken)
    {
        _db.PomodoroSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PomodoroSession?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken) =>
        _db.PomodoroSessions.SingleOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<PomodoroSession> Items, int TotalCount)> ListForUserAsync(
        int userId, int limit, int offset, CancellationToken cancellationToken)
    {
        var query = _db.PomodoroSessions.Where(s => s.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> CountCompletedFocusAsync(int userId, CancellationToken cancellationToken) =>
        _db.PomodoroSessions.CountAsync(
            s => s.UserId == userId && s.Type == SessionType.Foco && s.Status == SessionStatus.Concluido,
            cancellationToken);

    public Task<bool> ExistsOverlappingAsync(
        int userId, DateTime startedAt, DateTime completedAt, CancellationToken cancellationToken) =>
        _db.PomodoroSessions.AnyAsync(
            s => s.UserId == userId && s.StartedAt < completedAt && startedAt < s.CompletedAt,
            cancellationToken);

    public async Task<IReadOnlyList<PomodoroSession>> ListAllAsync(int userId, CancellationToken cancellationToken) =>
        await _db.PomodoroSessions.Where(s => s.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, int>> CountCompletedByTaskAsync(
        IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken)
    {
        if (taskIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await _db.PomodoroSessions
            .Where(s => s.TaskItemId.HasValue
                && taskIds.Contains(s.TaskItemId.Value)
                && s.Type == SessionType.Foco
                && s.Status == SessionStatus.Concluido)
            .GroupBy(s => s.TaskItemId!.Value)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count, cancellationToken);
    }
}
