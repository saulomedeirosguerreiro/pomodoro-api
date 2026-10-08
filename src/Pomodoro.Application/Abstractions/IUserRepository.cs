using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>Remove o usuário e, por cascata (FK), todas as suas sessões, tarefas e conquistas.</summary>
    Task DeleteAsync(User user, CancellationToken cancellationToken);

    /// <summary>Persiste alterações feitas em entidades já rastreadas (ex.: após chamar um método de domínio).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
