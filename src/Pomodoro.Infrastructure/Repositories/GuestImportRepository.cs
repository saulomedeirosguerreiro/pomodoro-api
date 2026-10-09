using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Repositories;

public sealed class GuestImportRepository : IGuestImportRepository
{
    private readonly PomodoroDbContext _db;

    public GuestImportRepository(PomodoroDbContext db)
    {
        _db = db;
    }

    public Task<GuestImport?> FindAsync(int userId, string guestId, CancellationToken cancellationToken) =>
        _db.GuestImports.SingleOrDefaultAsync(g => g.UserId == userId && g.GuestId == guestId, cancellationToken);

    /// <summary>
    /// Traduz a violação do índice único (UserId, GuestId) — a corrida documentada em D2/1.7 — para
    /// <see cref="DuplicateGuestImportException"/>, mantendo a Application sem depender de EF Core.
    /// Quando chamada dentro de uma transação ambiente (import em lote do modo guest), abre um SAVEPOINT
    /// antes de salvar: o Postgres, ao contrário do SQLite, aborta a transação inteira após qualquer
    /// erro — sem o SAVEPOINT, um guestId duplicado no meio do lote impediria o restante do import.
    /// </summary>
    public async Task AddAsync(GuestImport import, CancellationToken cancellationToken)
    {
        _db.GuestImports.Add(import);

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

            _db.Entry(import).State = EntityState.Detached;
            throw new DuplicateGuestImportException(
                "Esta importação já foi processada para este usuário (guestId duplicado).");
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
