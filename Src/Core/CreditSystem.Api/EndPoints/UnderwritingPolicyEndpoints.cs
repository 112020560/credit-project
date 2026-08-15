using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class UnderwritingPolicyEndpoints
{
    public static void MapUnderwritingPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/underwriting-policies")
            .WithTags("Underwriting Policies")
            .WithOpenApi();

        group.MapGet("/", GetAll)
            .WithName("GetUnderwritingPolicies")
            .WithSummary("List all available underwriting policies")
            .Produces<IEnumerable<UnderwritingPolicyResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAll(
        [FromServices] IUnderwritingPolicyRepository repository,
        CancellationToken cancellationToken)
    {
        var policies = await repository.GetAllAsync(cancellationToken);
        return Results.Ok(policies.Select(ToResponse));
    }

    private static UnderwritingPolicyResponse ToResponse(UnderwritingPolicy p) => new(
        p.Id,
        p.BaseInterestRate,
        p.MaxDtiRatio,
        p.SharesMultiplierLimit,
        p.EnforceSharesCapacityLimit,
        p.RequireActiveMembership,
        p.NoScoreBehavior.ToString(),
        p.GracePeriodDays,
        p.PenaltyRate,
        p.OriginationFeeRate,
        p.AutoDefaultThresholdDays);
}

public record UnderwritingPolicyResponse(
    string Id,
    decimal BaseInterestRate,
    decimal MaxDtiRatio,
    int SharesMultiplierLimit,
    bool EnforceSharesCapacityLimit,
    bool RequireActiveMembership,
    string NoScoreBehavior,
    int GracePeriodDays,
    decimal PenaltyRate,
    decimal OriginationFeeRate,
    int AutoDefaultThresholdDays);
