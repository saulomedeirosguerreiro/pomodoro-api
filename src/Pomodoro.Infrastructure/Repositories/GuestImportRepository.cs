using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Pomodoro.Infrastructure.Persistence;

namespace Pomodoro.Infrastructure.Repositories;

public sealed class GuestImportRepository : IGuestImportRepository
{
    // SQLITE_CONSTRAINT_UNIQUE — ver SqliteTransactionConstraintBehaviorTests para a prova empírica de
    // que uma violação assim aborta só esta instrução, não a transação. (Id é PK autoincrement — uma
    // violação de PRIMARYKEY não é um cenário real aqui, só a UNIQUE de (UserId, GuestId) importa.)
    private const int SqliteConstraintUniqueExtendedCode = 2067;

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
    /// </summary>
    public async Task AddAsync(GuestImport import, CancellationToken cancellationToken)
    {
        _db.GuestImports.Add(import);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateGuestImportException(
                "Esta importação já foi processada para este usuário (guestId duplicado).");
        }
    }

    /// <summary>
    /// <c>internal</c> (não <c>private</c>) só para permitir um teste direto, sem banco, do caso em que
    /// a falha NÃO veio do SQLite (ex.: <see cref="DbUpdateException.InnerException"/> de outra origem) —
    /// difícil de forçar de ponta a ponta via uma violação real de constraint.
    /// </summary>
    internal static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUniqueExtendedCode };
}
