namespace Pomodoro.Application.Common;

/// <summary>Abstrai o relógio para permitir testes determinísticos nos handlers.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
