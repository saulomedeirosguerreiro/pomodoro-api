namespace Pomodoro.Application.Common.Exceptions;

/// <summary>
/// Corrida perdida na importação em lote (D2): outra requisição com o mesmo (UserId, GuestId) já
/// commitou primeiro no índice único de `guest_imports`. Traduzida pela Infrastructure a partir da
/// violação de constraint do provedor (Postgres) — a Application nunca referencia EF Core diretamente
/// (mesmo motivo de existir de <see cref="Abstractions.IUnitOfWork"/>/<see cref="Abstractions.IAppTransaction"/>).
/// Nunca mapeada para um status HTTP próprio: <c>ImportGuestDataHandler</c> sempre a captura e devolve
/// o relatório já salvo pelo vencedor da corrida, com o mesmo contrato 200 OK do caminho feliz.
/// </summary>
public sealed class DuplicateGuestImportException : Exception
{
    public DuplicateGuestImportException(string message) : base(message)
    {
    }
}
