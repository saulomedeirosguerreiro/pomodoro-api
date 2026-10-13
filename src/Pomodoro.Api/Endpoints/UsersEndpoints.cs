using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Pomodoro.Api.Common;
using Pomodoro.Application.Users.ChangePassword;
using Pomodoro.Application.Users.Delete;
using Pomodoro.Application.Users.GetProfile;
using Pomodoro.Application.Users.UpdateName;

namespace Pomodoro.Api.Endpoints;

public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/me", async (
            ClaimsPrincipal user, GetUserProfileHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(user.GetUserId(), cancellationToken);
            return Results.Ok(response);
        });

        group.MapDelete("/me", async (
                [FromBody] DeleteAccountRequest request,
                ClaimsPrincipal user,
                DeleteAccountHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<DeleteAccountRequest>>();

        group.MapPatch("/me", async (
                [FromBody] UpdateNameRequest request,
                ClaimsPrincipal user,
                UpdateNameHandler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<UpdateNameRequest>>();

        group.MapPatch("/me/password", async (
                [FromBody] ChangePasswordRequest request,
                ClaimsPrincipal user,
                ChangePasswordHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<ChangePasswordRequest>>();

        return group;
    }
}
