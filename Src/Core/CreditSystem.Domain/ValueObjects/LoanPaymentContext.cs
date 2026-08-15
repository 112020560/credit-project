namespace CreditSystem.Domain.ValueObjects;

/// <summary>
/// Snapshot of the payable balances for each component at the time a payment is being applied.
/// </summary>
public record LoanPaymentContext(
    Money TotalFees,
    Money PenaltyInterest,
    Money AccruedInterest,
    Money Principal,
    string Currency);
