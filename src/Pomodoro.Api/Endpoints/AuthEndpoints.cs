using Pomodoro.Api.Common;
using Pomodoro.Application.Auth.Login;
using Pomodoro.Application.Auth.RecoverPassword;
using Pomodoro.Application.Auth.Register;

namespace Pomodoro.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (
                RegisterUserRequest request, RegisterUserHandler handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(request, cancellationToken);
                return Results.Created($"/api/users/{response.Id}", response);
            })
            .AddEndpointFilter<ValidationFilter<RegisterUserRequest>>();

        group.MapPost("/login", async (
                LoginRequest request, LoginHandler handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost("/password-recovery", async (
                RecoverPasswordRequest request, RecoverPasswordHandler handler, CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(request, cancellationToken);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<RecoverPasswordRequest>>()
            .RequireRateLimiting(RateLimitPolicies.PasswordRecovery);

        return group;
    }
}
