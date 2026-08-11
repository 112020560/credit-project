using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.ValueObjects;

public record PaymentWaterfall
{
    public IReadOnlyList<PaymentWaterfallStep> Steps { get; init; }

    public PaymentWaterfall(IEnumerable<PaymentWaterfallStep> steps)
    {
        Steps = steps.OrderBy(s => s.Priority).ToList().AsReadOnly();
    }

    /// <summary>
    /// Legacy order: Fees → PenaltyInterest → RegularInterest → Principal.
    /// Used for loans without a product assigned.
    /// </summary>
    public static PaymentWaterfall Default => new([
        new(1, PaymentComponent.Fees),
        new(2, PaymentComponent.PenaltyInterest),
        new(3, PaymentComponent.RegularInterest),
        new(4, PaymentComponent.Principal)
    ]);

    /// <summary>
    /// Distributes <paramref name="amount"/> across loan components in priority order.
    /// Social capital must be handled externally before calling this method
    /// (deducted from amount if IncludedInPayment, or left intact if SeparateCollection).
    /// </summary>
    public PaymentDistribution Apply(Money amount, LoanPaymentContext context)
    {
        var remaining = amount.Amount;
        var currency = amount.Currency;

        var insurancePaid   = 0m;
        var feesPaid        = 0m;
        var penaltyPaid     = 0m;
        var interestPaid    = 0m;
        var principalPaid   = 0m;

        foreach (var step in Steps)
        {
            if (remaining <= 0m) break;

            switch (step.Component)
            {
                case PaymentComponent.Insurance:
                    // Insurance treated as fees-type charge (uses TotalFees balance)
                    var ins = Math.Min(remaining, context.TotalFees.Amount - feesPaid);
                    ins = Math.Max(0m, ins);
                    insurancePaid += ins;
                    remaining -= ins;
                    break;

                case PaymentComponent.Fees:
                    var fee = Math.Min(remaining, Math.Max(0m, context.TotalFees.Amount - insurancePaid));
                    feesPaid += fee;
                    remaining -= fee;
                    break;

                case PaymentComponent.PenaltyInterest:
                    var penalty = Math.Min(remaining, Math.Max(0m, context.PenaltyInterest.Amount));
                    penaltyPaid += penalty;
                    remaining -= penalty;
                    break;

                case PaymentComponent.RegularInterest:
                    var interest = Math.Min(remaining, Math.Max(0m, context.AccruedInterest.Amount));
                    interestPaid += interest;
                    remaining -= interest;
                    break;

                case PaymentComponent.SocialCapital:
                    // Handled externally — skip in distribution loop
                    break;

                case PaymentComponent.Principal:
                    var principal = Math.Min(remaining, Math.Max(0m, context.Principal.Amount));
                    principalPaid += principal;
                    remaining -= principal;
                    break;
            }
        }

        return new PaymentDistribution(
            InsurancePaid:      new Money(Math.Round(insurancePaid, 2), currency),
            FeesPaid:           new Money(Math.Round(feesPaid, 2), currency),
            PenaltyInterestPaid: new Money(Math.Round(penaltyPaid, 2), currency),
            InterestPaid:       new Money(Math.Round(interestPaid, 2), currency),
            SocialCapitalPaid:  Money.Zero(currency),
            PrincipalPaid:      new Money(Math.Round(principalPaid, 2), currency));
    }
}
