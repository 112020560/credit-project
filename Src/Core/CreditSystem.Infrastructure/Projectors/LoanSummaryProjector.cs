using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Infrastructure.Projections;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Projectors;

// Infrastructure/Projectors/LoanSummaryProjector.cs
public class LoanSummaryProjector : IProjection
{
    private readonly IProjectionStore _store;
    private readonly ICustomerService _customerService;
    private readonly ILogger<LoanSummaryProjector> _logger;

    public string ProjectionName => "LoanSummary";

    public LoanSummaryProjector(
        IProjectionStore store,
        ICustomerService customerService,
        ILogger<LoanSummaryProjector> logger)
    {
        _store = store;
        _customerService = customerService;
        _logger = logger;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        switch (@event)
        {
            case ContractCreated e:
                await HandleContractCreated(e, ct);
                break;
            case LoanDisbursed e:
                await HandleLoanDisbursed(e, ct);
                break;
            case InterestAccrued e:
                await HandleInterestAccrued(e, ct);
                break;
            case PaymentApplied e:
                await HandlePaymentApplied(e, ct);
                break;
            case PaymentMissed e:
                await HandlePaymentMissed(e, ct);
                break;
            case ContractDefaulted e:
                await HandleContractDefaulted(e, ct);
                break;
            case ContractPaidOff e:
                await HandleContractPaidOff(e, ct);
                break;
            case ContractRestructured e:
                await HandleContractRestructured(e, ct);
                break;
        }
    }

    private async Task HandleContractCreated(ContractCreated e, CancellationToken ct)
    {
        var customer = await _customerService.GetByIdAsync(e.CustomerId, ct);
        var firstPayment = e.Schedule.Entries.FirstOrDefault();

        var model = new LoanSummaryReadModel
        {
            LoanId = e.AggregateId,
            CustomerId = e.CustomerId,
            CustomerName = customer?.FullName,
            Principal = e.Principal.Amount,
            CurrentBalance = e.Principal.Amount,
            AccruedInterest = 0,
            TotalFees = 0,
            InterestRate = e.InterestRate.AnnualRate,
            TermMonths = e.TermMonths,
            Status = "Approved",
            PaymentsMade = 0,
            PaymentsMissed = 0,
            NextPaymentDate = firstPayment?.DueDate,
            NextPaymentAmount = firstPayment?.TotalPayment.Amount,
            CreatedAt = e.OccurredAt,
            Version = e.Version,
            UpdatedAt = DateTime.UtcNow
        };

        await _store.UpsertAsync("rm_loan_summaries", model, "loan_id", ct);
        
        _logger.LogDebug("Projected ContractCreated for loan {LoanId}", e.AggregateId);
    }

    private async Task HandleLoanDisbursed(LoanDisbursed e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_loan_summaries 
            SET status = 'Active', 
                disbursed_at = @DisbursedAt, 
                version = @Version,
                updated_at = @Now
            WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            DisbursedAt = e.DisbursedAt,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);
    }

    private async Task HandleInterestAccrued(InterestAccrued e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_loan_summaries 
            SET accrued_interest = accrued_interest + @Amount,
                last_interest_accrual_date = @PeriodEnd,
                version = @Version,
                updated_at = @Now
            WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            Amount = e.Amount.Amount,
            PeriodEnd = e.PeriodEnd,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);
    }

    private async Task HandlePaymentApplied(PaymentApplied e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_loan_summaries 
            SET current_balance = @NewBalance,
                accrued_interest = accrued_interest - @InterestPaid,
                total_fees = total_fees - @FeesPaid,
                payments_made = @PaymentNumber,
                last_payment_at = @Now,
                version = @Version,
                updated_at = @Now
            WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            NewBalance = e.NewBalance.Amount,
            InterestPaid = e.InterestPaid.Amount,
            FeesPaid = e.FeePaid.Amount,
            PaymentNumber = e.PaymentNumber,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);
    }

    private async Task HandlePaymentMissed(PaymentMissed e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_loan_summaries 
            SET payments_missed = payments_missed + 1,
                total_fees = total_fees + @LateFee,
                status = 'Delinquent',
                version = @Version,
                updated_at = @Now
            WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            LateFee = e.LateFeeApplied.Amount,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);
    }

    private async Task HandleContractDefaulted(ContractDefaulted e, CancellationToken ct)
    {
        const string sql = @"
        UPDATE rm_loan_summaries 
        SET status = 'Default',
            defaulted_at = @DefaultedAt,
            version = @Version,
            updated_at = @Now
        WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            DefaultedAt = DateTime.UtcNow,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected ContractDefaulted for loan {LoanId}", e.AggregateId);
    }

    private async Task HandleContractPaidOff(ContractPaidOff e, CancellationToken ct)
    {
        const string sql = @"
        UPDATE rm_loan_summaries 
        SET status = 'PaidOff',
            current_balance = 0,
            accrued_interest = 0,
            total_fees = 0,
            paid_off_at = @PaidOffAt,
            version = @Version,
            updated_at = @Now
        WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            PaidOffAt = e.PaidOffAt,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected ContractPaidOff for loan {LoanId}", e.AggregateId);
    }

    private async Task HandleContractRestructured(ContractRestructured e, CancellationToken ct)
    {
        var firstPayment = e.NewSchedule.Entries.FirstOrDefault();

        const string sql = @"
        UPDATE rm_loan_summaries 
        SET status = 'Active',
            interest_rate = @NewRate,
            term_months = @NewTermMonths,
            current_balance = current_balance - @ForgiveAmount,
            payments_missed = 0,
            next_payment_date = @NextPaymentDate,
            next_payment_amount = @NextPaymentAmount,
            version = @Version,
            updated_at = @Now
        WHERE loan_id = @LoanId";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            NewRate = e.NewRate.AnnualRate,
            NewTermMonths = e.NewTermMonths,
            ForgiveAmount = e.ForgiveAmount.Amount,
            NextPaymentDate = firstPayment?.DueDate,
            NextPaymentAmount = firstPayment?.TotalPayment.Amount,
            Version = e.Version,
            Now = DateTime.UtcNow
        }, ct);

        _logger.LogDebug("Projected ContractRestructured for loan {LoanId}", e.AggregateId);
    }

    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        // Limpiar tabla
        await _store.ExecuteAsync("TRUNCATE TABLE rm_loan_summaries", ct: ct);

        await foreach (var @event in events.WithCancellation(ct))
        {
            await ProjectAsync(@event, ct);
        }
    }
}