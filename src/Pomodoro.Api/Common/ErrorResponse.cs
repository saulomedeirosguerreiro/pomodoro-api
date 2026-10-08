namespace Pomodoro.Api.Common;

/// <summary>Envelope único de erro (Q-12): { "error": { "code", "message", "fields"? } }.</summary>
public sealed record ErrorResponse(ErrorBody Error);

public sealed record ErrorBody(string Code, string Message, IReadOnlyList<FieldError>? Fields = null);

public sealed record FieldError(string Field, string Message);
