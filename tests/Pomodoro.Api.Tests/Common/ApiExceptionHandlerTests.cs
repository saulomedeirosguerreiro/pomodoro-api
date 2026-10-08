using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Pomodoro.Api.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Common;
using Xunit;

namespace Pomodoro.Api.Tests.Common;

public class ApiExceptionHandlerTests
{
    private readonly ApiExceptionHandler _handler = new(NullLogger<ApiExceptionHandler>.Instance);

    private static DefaultHttpContext BuildHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Theory]
    [MemberData(nameof(ExceptionToStatusCode))]
    public async Task TryHandleAsync_MapeiaCadaExcecaoParaOStatusCorreto(Exception exception, int expectedStatus)
    {
        var context = BuildHttpContext();

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(expectedStatus);
    }

    public static IEnumerable<object[]> ExceptionToStatusCode()
    {
        yield return new object[] { new ConflictException("conflito"), StatusCodes.Status409Conflict };
        yield return new object[] { new NotFoundException("não encontrado"), StatusCodes.Status404NotFound };
        yield return new object[] { new InvalidCredentialsException("credenciais"), StatusCodes.Status401Unauthorized };
        yield return new object[] { new FieldValidationException("campo", "inválido"), StatusCodes.Status422UnprocessableEntity };
        yield return new object[] { new DomainException("regra de domínio"), StatusCodes.Status422UnprocessableEntity };
        yield return new object[] { new InvalidOperationException("inesperado"), StatusCodes.Status500InternalServerError };
    }

    [Fact]
    public async Task TryHandleAsync_ComFieldValidationException_IncluiOCampoNoErro()
    {
        var context = BuildHttpContext();

        await _handler.TryHandleAsync(context, new FieldValidationException("tz", "Fuso horário inválido."), CancellationToken.None);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        body.Should().Contain("\"field\":\"tz\"").And.Contain("Fuso horário inválido.");
    }
}
