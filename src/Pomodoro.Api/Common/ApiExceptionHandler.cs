using Microsoft.AspNetCore.Diagnostics;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Common;

namespace Pomodoro.Api.Common;

/// <summary>
/// Converge toda exceção não tratada para o envelope único de erro (US-19, RNF-04).
/// Nenhum stack trace chega ao cliente; erros inesperados (500) são logados no servidor.
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, code, message, fields) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Erro não tratado ao processar {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(new ErrorBody(code, message, fields)), cancellationToken);

        return true;
    }

    private static (int StatusCode, string Code, string Message, IReadOnlyList<FieldError>? Fields) Map(Exception exception) =>
        exception switch
        {
            ConflictException ex => (StatusCodes.Status409Conflict, "conflict", ex.Message, null),
            NotFoundException ex => (StatusCodes.Status404NotFound, "not_found", ex.Message, null),
            InvalidCredentialsException ex => (StatusCodes.Status401Unauthorized, "unauthorized", ex.Message, null),
            FieldValidationException ex => (
                StatusCodes.Status422UnprocessableEntity,
                "validation_error",
                ex.Message,
                new[] { new FieldError(ex.Field, ex.Message) }),
            DomainException ex => (StatusCodes.Status422UnprocessableEntity, "validation_error", ex.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "internal_error", "Ocorreu um erro inesperado.", null),
        };
}
