using System.Security.Claims;
using Pomodoro.Api.Common;
using Pomodoro.Application.Progress.GetProgress;

namespace Pomodoro.Api.Endpoints;

public static class ProgressEndpoints
{
    public static RouteGroupBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me").RequireAuthorization();

        group.MapGet("/progress", async (
            ClaimsPrincipal user, string? tz, GetProgressHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(user.GetUserId(), tz, cancellationToken);
            return Results.Ok(response);
        });

        return group;
    }
}
