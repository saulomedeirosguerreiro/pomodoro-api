namespace Pomodoro.Application.Common.Exceptions;

/// <summary>
/// Corrida de concorrência (US-53 RN-02/1.7): outra requisição já desbloqueou a mesma conquista para o
/// mesmo usuário (índice único (UserId, Code) de `user_achievements`) entre a checagem em memória e o
/// insert. Traduzida pela Infrastructure a partir da violação de constraint do provedor (SQLite) — a
/// Application nunca referencia EF Core diretamente (mesmo motivo de existir de
/// <see cref="Abstractions.IUnitOfWork"/>/<see cref="Abstractions.IAppTransaction"/>). Tratada como
/// "já desbloqueada concorrentemente, ignorar" por quem credita conquistas (ex.: importação em lote).
/// </summary>
public sealed class DuplicateUserAchievementException : Exception
{
    public DuplicateUserAchievementException(string message) : base(message)
    {
    }
}
