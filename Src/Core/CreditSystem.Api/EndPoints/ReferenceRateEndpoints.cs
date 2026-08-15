using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class ReferenceRateEndpoints
{
    public static void MapReferenceRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reference-rates")
            .WithTags("Reference Rates")
            .WithOpenApi();

        group.MapGet("/{id}", GetReferenceRate)
            .WithName("GetReferenceRate")
            .WithSummary("Get the current value of a reference rate (e.g. TBP_CRC)")
            .Produces<ReferenceRateResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", UpdateReferenceRate)
            .WithName("UpdateReferenceRate")
            .WithSummary("Create or update a reference rate")
            .Produces<ReferenceRateResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetReferenceRate(
        string id,
        [FromServices] IReferenceRateRepository repository,
        CancellationToken cancellationToken)
    {
        var entry = await repository.GetCurrentAsync(id, cancellationToken);
        return entry is null
            ? Results.NotFound()
            : Results.Ok(ReferenceRateResponse.From(entry));
    }

    private static async Task<IResult> UpdateReferenceRate(
        string id,
        [FromBody] UpdateReferenceRateRequest request,
        [FromServices] IReferenceRateRepository repository,
        CancellationToken cancellationToken)
    {
        if (request.CurrentValue < 0 || request.CurrentValue > 100)
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "CurrentValue must be between 0 and 100",
                Status = StatusCodes.Status400BadRequest
            });

        if (string.IsNullOrWhiteSpace(request.Source))
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "Source is required",
                Status = StatusCodes.Status400BadRequest
            });

        var existing = await repository.GetCurrentAsync(id, cancellationToken);
        var name = existing?.Name ?? id;

        var entry = new ReferenceRateEntry
        {
            Id = id,
            Name = name,
            CurrentValue = request.CurrentValue,
            EffectiveDate = request.EffectiveDate.ToUniversalTime(),
            Source = request.Source,
            UpdatedAt = DateTime.UtcNow
        };

        await repository.UpsertAsync(entry, cancellationToken);
        return Results.Ok(ReferenceRateResponse.From(entry));
    }
}

public record ReferenceRateResponse(
    string Id,
    string Name,
    decimal CurrentValue,
    DateTime EffectiveDate,
    string Source,
    DateTime UpdatedAt)
{
    public static ReferenceRateResponse From(ReferenceRateEntry e) =>
        new(e.Id, e.Name, e.CurrentValue, e.EffectiveDate, e.Source, e.UpdatedAt);
}

public record UpdateReferenceRateRequest(
    decimal CurrentValue,
    DateTime EffectiveDate,
    string Source);
