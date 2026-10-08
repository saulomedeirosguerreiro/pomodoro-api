namespace Pomodoro.Application.Common.Exceptions;

/// <summary>Mapeada para HTTP 404 na Api. Usada tanto para "não existe" quanto "não é do dono" (L-11).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
