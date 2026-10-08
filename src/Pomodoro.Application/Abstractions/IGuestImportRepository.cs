using Pomodoro.Domain.Entities;

namespace Pomodoro.Application.Abstractions;

/// <summary>Idempotência da importação em lote (D2): um registro por (UserId, GuestId).</summary>
public interface IGuestImportRepository
{
    Task<GuestImport?> FindAsync(int userId, string guestId, CancellationToken cancellationToken);

    Task AddAsync(GuestImport import, CancellationToken cancellationToken);
}
