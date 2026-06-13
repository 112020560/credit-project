using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Job;

public class RevolvingInterestAccrualJob : IRevolvingInterestAccrualJob
{
    private readonly IRevolvingCreditQueryService _queryService;
    private readonly IRevolvingCreditRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<RevolvingInterestAccrualJob> _logger;

    public RevolvingInterestAccrualJob(
        IRevolvingCreditQueryService queryService,
        IRevolvingCreditRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<RevolvingInterestAccrualJob> logger)
    {
        _queryService = queryService;
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting revolving interest accrual job at {Time}", DateTime.UtcNow);

        var activeCreditLines = await _queryService.GetActiveForInterestAccrualAsync(cancellationToken);

        _logger.LogInformation("Found {Count} active credit lines for interest accrual", activeCreditLines.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var creditLine in activeCreditLines)
        {
            try
            {
                await AccrueInterestForCreditLineAsync(creditLine.CreditLineId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to accrue interest for credit line {CreditLineId}", creditLine.CreditLineId);
            }
        }

        _logger.LogInformation(
            "Revolving interest accrual job completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }

    private async Task AccrueInterestForCreditLineAsync(Guid creditLineId, CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(creditLineId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Credit line {CreditLineId} not found for interest accrual", creditLineId);
            return;
        }

        if (aggregate.State.CurrentBalance.Amount <= 0)
        {
            _logger.LogDebug("Credit line {CreditLineId} has zero balance, skipping interest accrual", creditLineId);
            return;
        }

        var periodEnd = DateTime.UtcNow.Date;
        var periodStart = aggregate.State.LastInterestAccrualDate?.Date
            ?? aggregate.State.ActivatedAt?.Date
            ?? periodEnd.AddDays(-1);

        // Asegurar al menos 1 día de diferencia
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
            _logger.LogWarning(
                "Cannot accrue interest for credit line {CreditLineId}: {Error}",
                creditLineId, ex.Message);
            return;
        }

        if (!aggregate.UncommittedEvents.Any())
        {
            _logger.LogDebug("No interest accrued for credit line {CreditLineId}", creditLineId);
            return;
        }

        var events = aggregate.UncommittedEvents.ToList();
        await _repository.SaveAsync(aggregate, cancellationToken);

        foreach (var @event in events)
        {
            await _projectionEngine.ProjectEventAsync(@event, cancellationToken);
        }

        _logger.LogDebug(
            "Interest accrued for credit line {CreditLineId}: {Amount}",
            creditLineId, aggregate.State.AccruedInterest.Amount);
    }
}