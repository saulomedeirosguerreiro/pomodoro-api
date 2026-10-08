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
        var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
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
