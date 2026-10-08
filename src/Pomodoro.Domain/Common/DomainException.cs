namespace Pomodoro.Domain.Common;

/// <summary>Violação de uma invariante que nunca deveria ser quebrada, independente de quem chama o domínio.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
