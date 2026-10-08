using System.Security.Claims;
using Pomodoro.Api.Common;
using Pomodoro.Application.Achievements.List;

namespace Pomodoro.Api.Endpoints;

public static class AchievementsEndpoints
{
    public static RouteGroupBuilder MapAchievementsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/achievements").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user, ListAchievementsHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(user.GetUserId(), cancellationToken);
            return Results.Ok(response);
        });

        return group;
    }
}
