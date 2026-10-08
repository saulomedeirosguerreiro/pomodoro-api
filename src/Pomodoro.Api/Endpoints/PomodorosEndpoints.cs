using System.Security.Claims;
using Pomodoro.Api.Common;
using Pomodoro.Application.Pomodoros.Create;
using Pomodoro.Application.Pomodoros.GetById;
using Pomodoro.Application.Pomodoros.List;

namespace Pomodoro.Api.Endpoints;

public static class PomodorosEndpoints
{
    public static RouteGroupBuilder MapPomodorosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pomodoros").RequireAuthorization();

        group.MapPost("/", async (
                CreatePomodoroRequest request,
                ClaimsPrincipal user,
                CreatePomodoroHandler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return Results.Created($"/api/pomodoros/{response.Id}", response);
            })
            .AddEndpointFilter<ValidationFilter<CreatePomodoroRequest>>();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            int? limit,
            int? offset,
            ListPomodorosHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(user.GetUserId(), limit, offset, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:int}", async (
            int id, ClaimsPrincipal user, GetPomodoroByIdHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
            return Results.Ok(response);
        });

        return group;
    }
}
