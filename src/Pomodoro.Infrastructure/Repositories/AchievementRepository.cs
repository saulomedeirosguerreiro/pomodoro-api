using Microsoft.EntityFrameworkCore;
using Pomodoro.Application.Abstractions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Repositories;

public sealed class AchievementRepository : IAchievementRepository
{
    private readonly PomodoroDbContext _db;

    public AchievementRepository(PomodoroDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserAchievement>> ListForUserAsync(int userId, CancellationToken cancellationToken) =>
        await _db.UserAchievements.Where(a => a.UserId == userId).ToListAsync(cancellationToken);

    public async Task AddAsync(UserAchievement achievement, CancellationToken cancellationToken)
    {
        _db.UserAchievements.Add(achievement);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
