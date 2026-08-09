using CreditSystem.Application.Configuration;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditSystem.Application.Job;

public class PaymentMissedJob : IPaymentMissedJob
{
    private readonly ILoanQueryService _queryService;
    private readonly ILoanContractRepository _repository;
    private readonly IProjectionEngine _projectionEngine;
    private readonly ILogger<PaymentMissedJob> _logger;
    private readonly LateFeeConfiguration _lateFeeConfig;
    private readonly UnderwritingPolicy _policy;

    public PaymentMissedJob(
        ILoanQueryService queryService,
        ILoanContractRepository repository,
        IProjectionEngine projectionEngine,
        IOptions<LateFeeConfiguration> lateFeeConfig,
        ILogger<PaymentMissedJob> logger,
        UnderwritingPolicy policy)
    {
        _queryService = queryService;
        _repository = repository;
        _projectionEngine = projectionEngine;
        _lateFeeConfig = lateFeeConfig.Value;
        _logger = logger;
        _policy = policy;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting payment missed detection job at {Time}", DateTime.UtcNow);

        var overdueLoans = await _queryService.GetLoansWithOverduePaymentsAsync(cancellationToken);

        _logger.LogInformation("Found {Count} loans with overdue payments", overdueLoans.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var loan in overdueLoans)
        {
            try
            {
                await ProcessOverduePaymentAsync(loan, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Failed to process overdue payment for loan {LoanId}", loan.LoanId);
            }
        }

        _logger.LogInformation(
            "Payment missed job completed. Success: {Success}, Errors: {Errors}",
            successCount, errorCount);
    }

    private async Task ProcessOverduePaymentAsync(
        OverdueLoanInfo loan,
        CancellationToken cancellationToken)
    {
        var aggregate = await _repository.GetByIdAsync(loan.LoanId, cancellationToken);

        if (aggregate == null)
        {
            _logger.LogWarning("Loan {LoanId} not found for overdue processing", loan.LoanId);
            return;
        }

        // Grace period guard: skip if not yet past the grace window
        if (loan.DaysOverdue <= _policy.GracePeriodDays)
        {
            _logger.LogDebug(
                "Loan {LoanId} is within grace period ({DaysOverdue} days overdue, grace = {Grace}). Skipping.",
                loan.LoanId, loan.DaysOverdue, _policy.GracePeriodDays);
            return;
        }

        var lateFee = CalculateLateFee(loan);

        // Penalty interest: principal × (penaltyRate/100/365) × daysOverdue
        var penaltyInterest = Money.Zero(loan.Currency);
        if (_policy.PenaltyRate > 0)
        {
            var overduePrincipal = aggregate.State.CurrentBalance.Amount;
            var penaltyAmount = overduePrincipal * (_policy.PenaltyRate / 100m / 365m) * loan.DaysOverdue;
            penaltyInterest = new Money(penaltyAmount, loan.Currency);
        }

        try
        {
            aggregate.RecordMissedPayment(
                loan.PaymentNumber,
                loan.DueDate,
                lateFee,
                _policy.AutoDefaultThresholdDays,
                penaltyInterest);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Cannot record missed payment for loan {LoanId}: {Error}",
                loan.LoanId, ex.Message);
            return;
        }

        // 4. Persistir
        await _repository.SaveAsync(aggregate, cancellationToken);

        // 5. Proyectar
        foreach (var @event in aggregate.UncommittedEvents)
        {
            await _projectionEngine.ProjectEventAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Recorded missed payment #{PaymentNumber} for loan {LoanId}. Days overdue: {DaysOverdue}, Late fee: {LateFee}, Penalty interest: {PenaltyInterest}",
            loan.PaymentNumber,
            loan.LoanId,
            loan.DaysOverdue,
            lateFee.Amount,
            penaltyInterest.Amount);
    }

    private Money CalculateLateFee(OverdueLoanInfo loan)
    {
        // Option 1: percentage of payment
        var percentageFee = loan.AmountDue * (_lateFeeConfig.PercentageOfPayment / 100);

        // Option 2: fixed amount
        var fixedFee = _lateFeeConfig.FixedAmount;

        // Option 3: daily charge
        var dailyFee = loan.DaysOverdue * _lateFeeConfig.DailyAmount;

        // Use the greater of percentage or fixed, plus the daily charge
        var baseFee = Math.Max(percentageFee, fixedFee);
        var totalFee = baseFee + dailyFee;

        // Apply maximum cap
        var cappedFee = Math.Min(totalFee, _lateFeeConfig.MaximumFee);

        return new Money(cappedFee, loan.Currency);
    }
}
