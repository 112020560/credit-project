using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Infrastructure.Projections;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .WithOpenApi();

        // POST /api/admin/jobs/interest-accrual - Ejecutar manualmente
        group.MapPost("/jobs/interest-accrual", RunInterestAccrualJob)
            .WithName("RunInterestAccrualJob")
            .WithSummary("Manually trigger interest accrual job")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

        // POST /api/admin/loans/{id}/accrue-interest - Acumular interés a un préstamo específico
        group.MapPost("/loans/{id:guid}/accrue-interest", AccrueInterestForLoan)
            .WithName("AccrueInterestForLoan")
            .WithSummary("Manually accrue interest for a specific loan")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        
        group.MapPost("/jobs/payment-missed", RunPaymentMissedJob)
            .WithName("RunPaymentMissedJob")
            .WithSummary("Manually trigger payment missed detection job")
            .Produces(StatusCodes.Status200OK);
        
        // POST /api/admin/jobs/revolving-interest-accrual
        group.MapPost("/jobs/revolving-interest-accrual", RunRevolvingInterestAccrualJob)
            .WithName("RunRevolvingInterestAccrualJob")
            .WithSummary("Manually trigger revolving interest accrual job")
            .Produces(StatusCodes.Status200OK);

// POST /api/admin/jobs/statement-generation
        group.MapPost("/jobs/statement-generation", RunStatementGenerationJob)
            .WithName("RunStatementGenerationJob")
            .WithSummary("Manually trigger statement generation job")
            .Produces(StatusCodes.Status200OK);

// POST /api/admin/jobs/revolving-payment-missed
        group.MapPost("/jobs/revolving-payment-missed", RunRevolvingPaymentMissedJob)
            .WithName("RunRevolvingPaymentMissedJob")
            .WithSummary("Manually trigger revolving payment missed job")
            .Produces(StatusCodes.Status200OK);

// POST /api/admin/revolving-credits/{id}/accrue-interest
        group.MapPost("/revolving-credits/{id:guid}/accrue-interest", AccrueInterestForCreditLine)
            .WithName("AccrueInterestForCreditLine")
            .WithSummary("Manually accrue interest for a specific credit line")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

// POST /api/admin/revolving-credits/{id}/generate-statement
        group.MapPost("/revolving-credits/{id:guid}/generate-statement", GenerateStatementForCreditLine)
            .WithName("GenerateStatementForCreditLine")
            .WithSummary("Manually generate statement for a specific credit line")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
    
    private static async Task<IResult> RunPaymentMissedJob(
        [FromServices] IPaymentMissedJob job,
        CancellationToken cancellationToken)
    {
        await job.ExecuteAsync(cancellationToken);
        return Results.Ok(new { Message = "Payment missed job completed" });
    }

    private static async Task<IResult> RunInterestAccrualJob(
        [FromServices] IInterestAccrualJob job,
        CancellationToken cancellationToken)
    {
        await job.ExecuteAsync(cancellationToken);
        return Results.Ok(new { Message = "Interest accrual job completed" });
    }

    private static async Task<IResult> AccrueInterestForLoan(
        Guid id,
        [FromServices] ILoanContractRepository repository,
        [FromServices] ProjectionEngine projectionEngine,
        CancellationToken cancellationToken)
    {
        var aggregate = await repository.GetByIdAsync(id, cancellationToken);

        if (aggregate == null)
            return Results.NotFound();

        var periodEnd = DateTime.UtcNow.Date;
        var periodStart = aggregate.State.LastInterestAccrualDate?.Date 
            ?? aggregate.State.DisbursedAt?.Date 
            ?? periodEnd.AddDays(-1);

        try
        {
            aggregate.AccrueInterest(periodStart, periodEnd);
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { Error = ex.Message });
        }

        await repository.SaveAsync(aggregate, cancellationToken);

        foreach (var @event in aggregate.UncommittedEvents)
        {
            await projectionEngine.ProjectEventAsync(@event, cancellationToken);
        }

        return Results.Ok(new
        {
            LoanId = id,
            AccruedInterest = aggregate.State.AccruedInterest.Amount,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });
    }
    
    private static async Task<IResult> RunRevolvingInterestAccrualJob(
    [FromServices] IRevolvingInterestAccrualJob job,
    CancellationToken cancellationToken)
{
    await job.ExecuteAsync(cancellationToken);
    return Results.Ok(new { Message = "Revolving interest accrual job completed" });
}

private static async Task<IResult> RunStatementGenerationJob(
    [FromServices] IStatementGenerationJob job,
    CancellationToken cancellationToken)
{
    await job.ExecuteAsync(cancellationToken);
    return Results.Ok(new { Message = "Statement generation job completed" });
}

private static async Task<IResult> RunRevolvingPaymentMissedJob(
    [FromServices] IRevolvingPaymentMissedJob job,
    CancellationToken cancellationToken)
{
    await job.ExecuteAsync(cancellationToken);
    return Results.Ok(new { Message = "Revolving payment missed job completed" });
}

private static async Task<IResult> AccrueInterestForCreditLine(
    Guid id,
    [FromServices] IRevolvingCreditRepository repository,
    [FromServices] ProjectionEngine projectionEngine,
    CancellationToken cancellationToken)
{
    var aggregate = await repository.GetByIdAsync(id, cancellationToken);

    if (aggregate == null)
        return Results.NotFound();

    var periodEnd = DateTime.UtcNow.Date;
    var periodStart = aggregate.State.LastInterestAccrualDate?.Date
        ?? aggregate.State.ActivatedAt?.Date
        ?? periodEnd.AddDays(-1);

    if (periodStart >= periodEnd)
    {
        periodStart = periodEnd.AddDays(-1);
    }

    try
    {
        aggregate.AccrueInterest(periodStart, periodEnd);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }

    var events = aggregate.UncommittedEvents.ToList();
    
    if (!events.Any())
    {
        return Results.Ok(new
        {
            CreditLineId = id,
            Message = "No interest to accrue (zero balance or already accrued)"
        });
    }

    await repository.SaveAsync(aggregate, cancellationToken);

    foreach (var @event in events)
    {
        await projectionEngine.ProjectEventAsync(@event, cancellationToken);
    }

    return Results.Ok(new
    {
        CreditLineId = id,
        AccruedInterest = aggregate.State.AccruedInterest.Amount,
        PeriodStart = periodStart,
        PeriodEnd = periodEnd
    });
}

private static async Task<IResult> GenerateStatementForCreditLine(
    Guid id,
    [FromServices] IRevolvingCreditRepository repository,
    [FromServices] ProjectionEngine projectionEngine,
    CancellationToken cancellationToken)
{
    var aggregate = await repository.GetByIdAsync(id, cancellationToken);

    if (aggregate == null)
        return Results.NotFound();

    try
    {
        aggregate.GenerateStatement();
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }

    var events = aggregate.UncommittedEvents.ToList();
    await repository.SaveAsync(aggregate, cancellationToken);

    foreach (var @event in events)
    {
        await projectionEngine.ProjectEventAsync(@event, cancellationToken);
    }

    var statementEvent = events.OfType<StatementGenerated>().First();

    return Results.Ok(new
    {
        CreditLineId = id,
        StatementId = statementEvent.StatementId,
        StatementDate = statementEvent.StatementDate,
        DueDate = statementEvent.DueDate,
        NewBalance = statementEvent.NewBalance.Amount,
        MinimumPayment = statementEvent.MinimumPayment.Amount
    });
}
}