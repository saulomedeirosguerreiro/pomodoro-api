using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pomodoro.Api.Common;
using Xunit;

namespace Pomodoro.Api.Tests.Common;

public class ValidationFilterTests
{
    private sealed record Dummy(string Name);

    private static EndpointFilterInvocationContext BuildContext(IServiceProvider services, Dummy argument) =>
        new DefaultEndpointFilterInvocationContext(
            new DefaultHttpContext { RequestServices = services }, argument);

    [Fact]
    public async Task InvokeAsync_SemValidatorRegistrado_ChamaProximoDelegateDireto()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var context = BuildContext(services, new Dummy("qualquer"));
        var filter = new ValidationFilter<Dummy>();
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_ComValidatorRegistradoEDadosInvalidos_Retorna422SemChamarProximo()
    {
        var services = new ServiceCollection()
            .AddSingleton<IValidator<Dummy>>(new InlineValidator<Dummy>
            {
                v => v.RuleFor(x => x.Name).NotEmpty()
            })
            .BuildServiceProvider();
        var context = BuildContext(services, new Dummy(string.Empty));
        var filter = new ValidationFilter<Dummy>();
        var nextCalled = false;

        await filter.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        nextCalled.Should().BeFalse();
    }
}
