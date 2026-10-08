using System.Security.Claims;
using Pomodoro.Api.Common;
using Pomodoro.Application.Users.GetProfile;

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

        return group;
    }
}
