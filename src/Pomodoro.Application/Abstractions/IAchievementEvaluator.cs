namespace Pomodoro.Application.Abstractions;

/// <summary>
/// Avalia o catálogo de conquistas (US-53) e persiste as novas. Interface só para permitir
/// dublagem em testes de quem dispara a avaliação (CreatePomodoroHandler, SetTaskStatusHandler).
/// </summary>
public interface IAchievementEvaluator
{
    /// <returns>Códigos das conquistas recém-desbloqueadas nesta chamada.</returns>
    Task<IReadOnlyList<string>> HandleAsync(int userId, CancellationToken cancellationToken);
}
