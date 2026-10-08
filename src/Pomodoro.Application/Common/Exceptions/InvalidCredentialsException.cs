namespace Pomodoro.Application.Common.Exceptions;

/// <summary>Mapeada para HTTP 401 na Api. Mensagem sempre genérica (L-15).</summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
