using System.Security.Claims;
using Pomodoro.Api.Common;
using Pomodoro.Application.Migration.Import;

namespace Pomodoro.Api.Endpoints;

public static class MigrationEndpoints
{
    public static RouteGroupBuilder MapMigrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/migration").RequireAuthorization();

        group.MapPost("/import", async (
                ImportGuestDataRequest request, ClaimsPrincipal user,
                ImportGuestDataHandler handler, CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<ImportGuestDataRequest>>();

        return group;
    }
}
