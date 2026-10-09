using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
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

    /// <summary>
    /// Traduz a violação do índice único (UserId, Code) para <see cref="DuplicateUserAchievementException"/>,
    /// mantendo a Application sem depender de EF Core. Quando chamada dentro de uma transação ambiente
    /// (import em lote do modo guest), abre um SAVEPOINT antes de salvar: o Postgres, ao contrário do
    /// SQLite, aborta a transação inteira após qualquer erro — sem o SAVEPOINT, uma conquista duplicada
    /// no meio do lote impediria as demais de serem persistidas.
    /// </summary>
    public async Task AddAsync(UserAchievement achievement, CancellationToken cancellationToken)
    {
        _db.UserAchievements.Add(achievement);

        var transaction = _db.Database.CurrentTransaction;
        var savepointName = transaction is not null ? $"sp_{Guid.NewGuid():N}" : null;
        if (transaction is not null)
        {
            await transaction.CreateSavepointAsync(savepointName!, cancellationToken);
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.ReleaseSavepointAsync(savepointName!, cancellationToken);
            }
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            if (transaction is not null)
            {
                await transaction.RollbackToSavepointAsync(savepointName!, cancellationToken);
            }

            _db.Entry(achievement).State = EntityState.Detached;
            throw new DuplicateUserAchievementException(
                "Esta conquista já foi desbloqueada concorrentemente para este usuário.");
        }
    }

    /// <summary>
    /// <c>internal</c> (não <c>private</c>) só para permitir um teste direto, sem banco, do caso em que
    /// a falha NÃO veio do Postgres (ex.: <see cref="DbUpdateException.InnerException"/> de outra origem) —
    /// difícil de forçar de ponta a ponta via uma violação real de constraint.
    /// </summary>
    internal static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
