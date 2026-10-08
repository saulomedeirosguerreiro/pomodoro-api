using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
