using Pomodoro.Api.Common;
using Pomodoro.Application.Auth.Login;
using Pomodoro.Application.Auth.Logout;
using Pomodoro.Application.Auth.RecoverPassword;
using Pomodoro.Application.Auth.Refresh;
using Pomodoro.Application.Auth.Register;
using Pomodoro.Application.Common.Exceptions;

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
            .AddEndpointFilter<ValidationFilter<RegisterUserRequest>>()
            .RequireRateLimiting(RateLimitPolicies.Register);

        group.MapPost("/login", async (
                LoginRequest request, LoginHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                AuthCookies.SetRefreshToken(httpContext, result.RefreshToken, result.RefreshTokenExpiresAt);
                return Results.Ok(new LoginResponse(result.AccessToken, result.User));
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost("/refresh", async (
                RefreshTokenHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var rawRefreshToken = AuthCookies.ReadRefreshToken(httpContext)
                    ?? throw new InvalidCredentialsException("Sessão expirada. Faça login novamente.");

                var result = await handler.HandleAsync(rawRefreshToken, cancellationToken);
                AuthCookies.SetRefreshToken(httpContext, result.RefreshToken, result.RefreshTokenExpiresAt);
                return Results.Ok(new RefreshResponse(result.AccessToken));
            });

        group.MapPost("/logout", async (
                LogoutHandler handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var rawRefreshToken = AuthCookies.ReadRefreshToken(httpContext);
                await handler.HandleAsync(rawRefreshToken, cancellationToken);
                AuthCookies.ClearRefreshToken(httpContext);
                return Results.NoContent();
            });

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

public sealed record RefreshResponse(string Token);
