using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Pomodoro.Application.Abstractions;

namespace Pomodoro.Infrastructure.Persistence;

/// <summary>Abre uma transação explícita do EF Core sobre o <see cref="PomodoroDbContext"/> atual.</summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly PomodoroDbContext _db;

    public EfUnitOfWork(PomodoroDbContext db)
    {
        _db = db;
    }

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        // Com `EnableRetryOnFailure` habilitado (ver `InfrastructureServiceCollectionExtensions`), o EF
        // Core recusa `BeginTransactionAsync` chamado fora de uma execution strategy — "does not support
        // user-initiated transactions" — por isso o `ExecuteAsync` aqui, só em volta da ABERTURA da
        // transação. Commit/Rollback continuam manuais, controlados por quem chamou (ex.:
        // `ImportGuestDataHandler`), sem mudar o contrato desta classe nem de `IUnitOfWork`. Isso cobre
        // o caso mais provável de falha transitória — a conexão caindo bem na hora de abrir, ex.: banco
        // ainda "acordando" de um cold start — retentando só esse passo.
        var strategy = _db.Database.CreateExecutionStrategy();
        var transaction = await strategy.ExecuteAsync(
            ct => _db.Database.BeginTransactionAsync(ct),
            cancellationToken);
        return new EfAppTransaction(transaction);
    }
}

internal sealed class EfAppTransaction : IAppTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfAppTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken) => _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
