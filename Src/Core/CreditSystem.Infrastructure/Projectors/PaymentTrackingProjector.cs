using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Infrastructure.Projections;

namespace CreditSystem.Infrastructure.Projectors;

/// <summary>
/// Projects payment-related domain events to the payment tracking read model.
/// This handles the async payment flow status updates from domain events.
/// </summary>
public class PaymentTrackingProjector : IProjection
{
    private readonly IProjectionStore _store;
    public string ProjectionName => "PaymentTracking";

    public PaymentTrackingProjector(IProjectionStore store)
    {
        _store = store;
    }

    public async Task ProjectAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        switch (@event)
        {
            case PaymentApplied e:
                await HandlePaymentAppliedAsync(e, ct);
                break;

            case RevolvingPaymentApplied e:
                await HandleRevolvingPaymentAppliedAsync(e, ct);
                break;

            case ContractPaidOff e:
                await HandleLoanPaidOffAsync(e, ct);
                break;
        }
    }

    private async Task HandlePaymentAppliedAsync(PaymentApplied e, CancellationToken ct)
    {
        // Update the tracking record when a payment is successfully applied
        // This is called by the consumer after the aggregate processes the payment
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = 'COMPLETED',
                principal_paid = @PrincipalPaid,
                interest_paid = @InterestPaid,
                fees_paid = @FeesPaid,
                new_balance = @NewBalance,
                updated_at = @UpdatedAt,
                processed_at = @ProcessedAt
            WHERE payment_id = @PaymentId
              AND status IN ('PENDING', 'PROCESSING')";

        await _store.ExecuteAsync(sql, new
        {
            PaymentId = e.PaymentId,
            PrincipalPaid = e.PrincipalPaid.Amount,
            InterestPaid = e.InterestPaid.Amount,
            FeesPaid = e.FeePaid.Amount,
            NewBalance = e.NewBalance.Amount,
            UpdatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = e.OccurredAt
        }, ct);
    }

    private async Task HandleRevolvingPaymentAppliedAsync(RevolvingPaymentApplied e, CancellationToken ct)
    {
        const string sql = @"
            UPDATE rm_payment_tracking
            SET status = 'COMPLETED',
                principal_paid = @PrincipalPaid,
                interest_paid = @InterestPaid,
                fees_paid = @FeesPaid,
                new_balance = @NewUsedCredit,
                new_available_credit = @NewAvailableCredit,
                updated_at = @UpdatedAt,
                processed_at = @ProcessedAt
            WHERE payment_id = @PaymentId
              AND status IN ('PENDING', 'PROCESSING')";

        await _store.ExecuteAsync(sql, new
        {
            PaymentId = e.PaymentId,
            PrincipalPaid = e.PrincipalPaid.Amount,
            InterestPaid = e.InterestPaid.Amount,
            FeesPaid = e.FeesPaid.Amount,
            NewUsedCredit = e.NewBalance.Amount,
            NewAvailableCredit = e.AvailableCredit.Amount,
            UpdatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = e.OccurredAt
        }, ct);
    }

    private async Task HandleLoanPaidOffAsync(ContractPaidOff e, CancellationToken ct)
    {
        // Mark the payment that triggered the payoff with is_paid_off = true
        const string sql = @"
            UPDATE rm_payment_tracking
            SET is_paid_off = true,
                updated_at = @UpdatedAt
            WHERE loan_id = @LoanId
              AND status = 'COMPLETED'
              AND processed_at = (
                  SELECT MAX(processed_at)
                  FROM rm_payment_tracking
                  WHERE loan_id = @LoanId AND status = 'COMPLETED'
              )";

        await _store.ExecuteAsync(sql, new
        {
            LoanId = e.AggregateId,
            UpdatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    public async Task RebuildAsync(IAsyncEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        // Payment tracking is a transactional log, not a projection that can be rebuilt
        // from domain events alone. Skip rebuild for this projector.
        // The tracking records are created by the SubmitPaymentCommandHandler
        // and updated by the consumers.
        await Task.CompletedTask;
    }
}
