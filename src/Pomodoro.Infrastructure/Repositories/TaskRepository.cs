using Microsoft.EntityFrameworkCore;
using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Entities;
using Pomodoro.Domain.Enums;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly PomodoroDbContext _db;

    public TaskRepository(PomodoroDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<TaskItem?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken) =>
        _db.Tasks.SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> ListForUserAsync(
        int userId, TaskItemStatus? status, CancellationToken cancellationToken)
    {
        var query = _db.Tasks.Where(t => t.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<TaskItem?> FindInFocusAsync(int userId, CancellationToken cancellationToken) =>
        _db.Tasks.SingleOrDefaultAsync(t => t.UserId == userId && t.Status == TaskItemStatus.EmCurso, cancellationToken);

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
