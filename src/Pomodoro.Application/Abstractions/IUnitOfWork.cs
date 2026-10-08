namespace Pomodoro.Application.Abstractions;

/// <summary>
/// Abre uma transação explícita compartilhada pelos repositórios já existentes — cada um continua
/// chamando <c>SaveChangesAsync</c> sozinho (só faz *flush* dentro da transação); o commit real só
/// acontece em <see cref="IAppTransaction.CommitAsync"/>. Usado pela importação em lote (F2) para
/// garantir "tudo ou nada" sem alterar nenhum repositório existente.
/// </summary>
public interface IUnitOfWork
{
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

/// <summary>Transação explícita aberta por <see cref="IUnitOfWork.BeginTransactionAsync"/>.</summary>
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
