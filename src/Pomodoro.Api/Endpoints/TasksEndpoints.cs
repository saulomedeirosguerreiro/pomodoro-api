using System.Security.Claims;
using Pomodoro.Api.Common;
using Pomodoro.Application.Tasks.Create;
using Pomodoro.Application.Tasks.Delete;
using Pomodoro.Application.Tasks.List;
using Pomodoro.Application.Tasks.SetStatus;
using Pomodoro.Application.Tasks.Update;

namespace Pomodoro.Api.Endpoints;

public static class TasksEndpoints
{
    public static RouteGroupBuilder MapTasksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").RequireAuthorization();

        group.MapPost("/", async (
                CreateTaskRequest request,
                ClaimsPrincipal user,
                CreateTaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(user.GetUserId(), request, cancellationToken);
                return Results.Created($"/api/tasks/{response.Id}", response);
            })
            .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>();

        group.MapGet("/", async (
            ClaimsPrincipal user, string? status, ListTasksHandler handler, CancellationToken cancellationToken) =>
        {
            var response = await handler.HandleAsync(user.GetUserId(), status, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPatch("/{id:int}", async (
                int id,
                UpdateTaskRequest request,
                ClaimsPrincipal user,
                UpdateTaskHandler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(id, user.GetUserId(), request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<UpdateTaskRequest>>();

        group.MapPatch("/{id:int}/status", async (
                int id,
                SetTaskStatusRequest request,
                ClaimsPrincipal user,
                SetTaskStatusHandler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(id, user.GetUserId(), request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<SetTaskStatusRequest>>();

        group.MapDelete("/{id:int}", async (
            int id, ClaimsPrincipal user, DeleteTaskHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(id, user.GetUserId(), cancellationToken);
            return Results.NoContent();
        });

        return group;
    }
}
