using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Job;

public class RateAdjustmentJob : IRateAdjustmentJob
{
    private readonly ILoanQueryService _queryService;
    private readonly ILoanContractRepository _repository;
    private readonly IReferenceRateRepository _referenceRateRepository;
    private readonly ILogger<RateAdjustmentJob> _logger;

    public RateAdjustmentJob(
        ILoanQueryService queryService,
        ILoanContractRepository repository,
        IReferenceRateRepository referenceRateRepository,
        ILogger<RateAdjustmentJob> logger)
    {
        _queryService = queryService;
        _repository = repository;
        _referenceRateRepository = referenceRateRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting rate adjustment job at {Time}", DateTime.UtcNow);

        var loans = await _queryService.GetActiveVariableRateLoansAsync(cancellationToken);

        _logger.LogInformation("Found {Count} active variable-rate loans", loans.Count);

        var adjusted = 0;
        var errors = 0;

        foreach (var loanInfo in loans)
        {
            try
            {
                var referenceRate = await _referenceRateRepository.GetCurrentAsync(
                    loanInfo.ReferenceRateId, cancellationToken);

                if (referenceRate == null)
                {
                    _logger.LogWarning(
                        "Reference rate '{ReferenceRateId}' not found for loan {LoanId} — skipping",
                        loanInfo.ReferenceRateId, loanInfo.LoanId);
                    continue;
                }

                var aggregate = await _repository.GetByIdAsync(loanInfo.LoanId, cancellationToken);
                if (aggregate == null)
                {
                    _logger.LogWarning("Loan {LoanId} not found in event store — skipping", loanInfo.LoanId);
                    continue;
                }

                aggregate.AdjustRate(referenceRate.CurrentValue, DateTime.UtcNow);

                if (!aggregate.UncommittedEvents.Any())
                {
                    _logger.LogDebug("Loan {LoanId}: rate unchanged (still {Rate}%)", loanInfo.LoanId, loanInfo.CurrentRate);
                    continue;
                }

                await _repository.SaveAsync(aggregate, cancellationToken);

                _logger.LogInformation(
                    "Loan {LoanId}: rate adjusted from {OldRate}% to {NewRate}% (ref: {RefId} = {RefVal}%)",
                    loanInfo.LoanId,
                    loanInfo.CurrentRate,
                    referenceRate.CurrentValue + loanInfo.Spread,
                    loanInfo.ReferenceRateId,
                    referenceRate.CurrentValue);

                adjusted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting rate for loan {LoanId}", loanInfo.LoanId);
                errors++;
            }
        }

        _logger.LogInformation(
            "Rate adjustment job complete. Adjusted: {Adjusted}, Errors: {Errors}, Total: {Total}",
            adjusted, errors, loans.Count);
    }
}
