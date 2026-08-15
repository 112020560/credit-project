namespace CreditSystem.Domain.ValueObjects;

public record PaymentDistribution(
    Money InsurancePaid,
    Money FeesPaid,
    Money PenaltyInterestPaid,
    Money InterestPaid,
    Money SocialCapitalPaid,
    Money PrincipalPaid)
{
    public Money Total =>
        new(InsurancePaid.Amount + FeesPaid.Amount + PenaltyInterestPaid.Amount
            + InterestPaid.Amount + SocialCapitalPaid.Amount + PrincipalPaid.Amount,
            FeesPaid.Currency);
}
