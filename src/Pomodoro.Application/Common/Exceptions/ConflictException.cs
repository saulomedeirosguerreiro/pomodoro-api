namespace Pomodoro.Application.Common.Exceptions;

/// <summary>Mapeada para HTTP 409 na Api.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
