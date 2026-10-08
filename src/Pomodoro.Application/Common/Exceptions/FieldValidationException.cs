namespace Pomodoro.Application.Common.Exceptions;

/// <summary>
/// Violação de uma regra de negócio ligada a um campo específico, detectada fora do FluentValidation
/// (ex.: precisa de acesso a repositório, como sobreposição de sessões). Mapeada para HTTP 422 na Api,
/// com o mesmo formato por-campo de uma falha de validação comum.
/// </summary>
public sealed class FieldValidationException : Exception
{
    public string Field { get; }

    public FieldValidationException(string field, string message) : base(message)
    {
        Field = field;
    }
}
