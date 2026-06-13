using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Job;

public class InterestAccrualJob : IInterestAccrualJob
{
    private readonly ILoanQueryService _queryService;
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<InterestAccrualJob> _logger;

    public InterestAccrualJob(
        ILoanQueryService queryService,
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        ILogger<InterestAccrualJob> logger)
    {
        _queryService = queryService;
        _repository = repository;
        _projectionEngine = projectionEngine;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting interest accrual job at {Time}", DateTime.UtcNow);

        // 1. Obtener préstamos activos
        var activeLoans = await _queryService.GetActiveLoansForAccrualAsync(cancellationToken);

        _logger.LogInformation("Found {Count} active loans for interest accrual", activeLoans.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var loan in activeLoans)
        {
            try
            {
                await AccrueInterestForLoanAsync(loan.LoanId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to accrue interest for loan {LoanId}", loan.LoanId);
            }
        }

        _logger.LogInformation(
            "Interest accrual job completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }

    private async Task AccrueInterestForLoanAsync(Guid loanId, CancellationToken cancellationToken)
    {
        // 1. Cargar aggregate
        var aggregate = await _repository.GetByIdAsync(loanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found for interest accrual", loanId);
            return;
        }

        // 2. Calcular período
        var periodEnd = DateTime.UtcNow.Date;
        var periodStart = aggregate.State.LastInterestAccrualDate?.Date ?? aggregate.State.DisbursedAt?.Date ?? periodEnd.AddDays(-1);

        // No acumular si ya se calculó hoy
        if (periodStart >= periodEnd)
        {
            _logger.LogDebug("Interest already accrued for loan {LoanId} today", loanId);
            return;
        }

        // 3. Ejecutar acumulación
        try
        {
            aggregate.AccrueInterest(periodStart, periodEnd);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot accrue interest for loan {LoanId}: {Error}",
                loanId, ex.Message);
            return;
        }

        // 4. Persistir
        await _repository.SaveAsync(aggregate, cancellationToken);

        // 5. Proyectar
        foreach (var @event in aggregate.UncommittedEvents)
        {
            await _projectionEngine.ProjectEventAsync(@event, cancellationToken);
        }

        _logger.LogDebug(
            "Interest accrued for loan {LoanId}: {Amount}",
            loanId, aggregate.State.AccruedInterest.Amount);
    }
}