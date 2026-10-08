using FluentValidation;

namespace Pomodoro.Api.Common;

/// <summary>
/// Roda o <see cref="IValidator{T}"/> registrado para o corpo da requisição antes do handler.
/// Em caso de falha, responde 422 no envelope único com um erro por campo (US-19, RNF-05, RNF-06).
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().First();

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(argument);
        if (!result.IsValid)
        {
            var fields = result.Errors
                .Select(e => new FieldError(e.PropertyName, e.ErrorMessage))
                .ToList();

            return Results.Json(
                new ErrorResponse(new ErrorBody("validation_error", "Dados inválidos.", fields)),
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return await next(context);
    }
}
