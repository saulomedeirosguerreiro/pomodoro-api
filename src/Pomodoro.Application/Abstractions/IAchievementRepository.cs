using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface IAchievementRepository
{
    Task<IReadOnlyList<UserAchievement>> ListForUserAsync(int userId, CancellationToken cancellationToken);

    Task AddAsync(UserAchievement achievement, CancellationToken cancellationToken);
}
