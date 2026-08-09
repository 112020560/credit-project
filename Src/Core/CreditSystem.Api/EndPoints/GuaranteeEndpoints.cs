using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class GuaranteeEndpoints
{
    public static void MapGuaranteeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loans/{loanContractId:guid}/guarantees")
            .WithTags("Loan Guarantees")
            .WithOpenApi();

        group.MapGet("/", GetGuaranteesByContract)
            .WithName("GetGuaranteesByContract")
            .WithSummary("List all guarantees for a loan contract")
            .Produces<IEnumerable<GuaranteeResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", RegisterGuarantee)
            .WithName("RegisterGuarantee")
            .WithSummary("Register a new guarantee for a loan contract")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{guaranteeId:guid}/status", UpdateGuaranteeStatus)
            .WithName("UpdateGuaranteeStatus")
            .WithSummary("Update the status of a guarantee")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetGuaranteesByContract(
        Guid loanContractId,
        [FromServices] ILoanGuaranteeRepository guaranteeRepository,
        [FromServices] ILoanQueryService queryService,
        CancellationToken cancellationToken)
    {
        var contract = await queryService.GetLoanSummaryAsync(loanContractId, cancellationToken);
        if (contract == null) return Results.NotFound();

        var guarantees = await guaranteeRepository.GetByContractAsync(loanContractId, cancellationToken);
        return Results.Ok(guarantees.Select(ToResponse));
    }

    private static async Task<IResult> RegisterGuarantee(
        Guid loanContractId,
        [FromBody] CreateGuaranteeRequest request,
        [FromServices] ILoanGuaranteeRepository guaranteeRepository,
        [FromServices] ILoanQueryService queryService,
        CancellationToken cancellationToken)
    {
        var contract = await queryService.GetLoanSummaryAsync(loanContractId, cancellationToken);
        if (contract == null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest("Description is required");
        if (request.AppraisalValue <= 0)
            return Results.BadRequest("Appraisal value must be greater than zero");
        if (request.CoverageRate <= 0 || request.CoverageRate > 1)
            return Results.BadRequest("Coverage rate must be between 0 (exclusive) and 1 (inclusive)");

        var valuation = new GuaranteeValuation(request.AppraisalValue, request.CoverageRate);
        var guarantee = new LoanGuarantee(
            Guid.NewGuid(),
            loanContractId,
            request.Type,
            request.Description,
            valuation,
            expirationDate: request.ExpirationDate);

        await guaranteeRepository.InsertAsync(guarantee, cancellationToken);

        return Results.Created($"/api/loans/{loanContractId}/guarantees/{guarantee.Id}", guarantee.Id);
    }

    private static async Task<IResult> UpdateGuaranteeStatus(
        Guid loanContractId,
        Guid guaranteeId,
        [FromBody] UpdateGuaranteeStatusRequest request,
        [FromServices] ILoanGuaranteeRepository guaranteeRepository,
        CancellationToken cancellationToken)
    {
        var guarantee = await guaranteeRepository.GetByIdAsync(guaranteeId, cancellationToken);
        if (guarantee == null || guarantee.LoanContractId != loanContractId) return Results.NotFound();

        await guaranteeRepository.UpdateStatusAsync(guaranteeId, request.Status, cancellationToken);
        return Results.NoContent();
    }

    private static GuaranteeResponse ToResponse(LoanGuarantee g) => new(
        g.Id,
        g.LoanContractId,
        g.Type.ToString(),
        g.Description,
        g.Valuation.AppraisalValue,
        g.Valuation.CoverageRate,
        g.Valuation.EffectiveCoverage,
        g.Status.ToString(),
        g.ExpirationDate);
}

public record CreateGuaranteeRequest(
    GuaranteeType Type,
    string Description,
    decimal AppraisalValue,
    decimal CoverageRate,
    DateOnly? ExpirationDate = null);

public record UpdateGuaranteeStatusRequest(GuaranteeStatus Status);

public record GuaranteeResponse(
    Guid Id,
    Guid LoanContractId,
    string Type,
    string Description,
    decimal AppraisalValue,
    decimal CoverageRate,
    decimal EffectiveCoverage,
    string Status,
    DateOnly? ExpirationDate);
