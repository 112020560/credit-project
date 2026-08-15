using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Models;
using CreditSystem.Infrastructure.Projectors;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class ProjectionAdminEndpoints
{
    public static void MapProjectionAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/projection")
            .WithTags("ProjectionAdmin")
            .WithOpenApi();

        group.MapGet("/failures", GetFailures)
            .WithName("GetProjectionFailures")
            .WithSummary("List projection failures (default: unresolved only)");

        group.MapPost("/failures/{id:guid}/resolve", ResolveFailure)
            .WithName("ResolveProjectionFailure")
            .WithSummary("Mark a projection failure as resolved");

        group.MapPost("/rebuild", RebuildProjections)
            .WithName("RebuildProjections")
            .WithSummary("Reset all checkpoints to 0 and truncate read models; worker replays from scratch");
    }

    private static async Task<IResult> GetFailures(
        [FromQuery] bool resolved = false,
        [FromServices] IProjectionFailureRepository failures = null!,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ProjectionFailure> records = resolved
            ? await failures.GetAllAsync(cancellationToken)
            : await failures.GetUnresolvedAsync(cancellationToken);

        var response = records.Select(f => new ProjectionFailureResponse(
            f.Id, f.EventId, f.StreamId, f.EventType, f.ProjectorName,
            f.ErrorMessage, f.OccurredAt, f.Attempts, f.Resolved, f.ResolvedAt));

        return Results.Ok(response);
    }

    private static async Task<IResult> ResolveFailure(
        Guid id,
        [FromServices] IProjectionFailureRepository failures,
        CancellationToken cancellationToken)
    {
        var resolved = await failures.ResolveAsync(id, cancellationToken);
        return resolved ? Results.Ok(new { Id = id, Resolved = true }) : Results.NotFound();
    }

    private static async Task<IResult> RebuildProjections(
        [FromServices] IEnumerable<IProjection> projectors,
        [FromServices] IProjectionCheckpointRepository checkpoints,
        CancellationToken cancellationToken)
    {
        var projectorList = projectors.ToList();

        // Each projector handles its own table truncation inside RebuildAsync.
        static async IAsyncEnumerable<CreditSystem.Domain.Abstractions.Events.IDomainEvent> Empty(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        foreach (var projector in projectorList)
        {
            await projector.RebuildAsync(Empty(cancellationToken), cancellationToken);
        }

        // Reset all checkpoints so the worker replays from sequence 0.
        foreach (var projector in projectorList)
        {
            await checkpoints.SaveCheckpointAsync(projector.ProjectionName, 0, cancellationToken);
        }

        return Results.Accepted("/api/admin/projection/failures", new
        {
            Message = "All read models truncated and checkpoints reset to 0. The projection worker will replay from the beginning.",
            Projectors = projectorList.Select(p => p.ProjectionName)
        });
    }
}

public record ProjectionFailureResponse(
    Guid Id,
    Guid EventId,
    Guid StreamId,
    string EventType,
    string ProjectorName,
    string ErrorMessage,
    DateTime OccurredAt,
    int Attempts,
    bool Resolved,
    DateTime? ResolvedAt);
