using System;

namespace Credit.Domain.DomainServices.AmortizationEngine;

public sealed class FrenchAmortizationStrategy : IAmortizationStrategy
{
    public CreditType CreditType => CreditType.French;

    public AmortizationSchedule Calculate(CreditTerms terms)
    {
        var installments = new List<Installment>();

        var monthlyRate = terms.AnnualInterestRate / 12m;
        var n = terms.NumberOfInstallments;
        var principal = terms.Principal;

        var factor = (decimal)Math.Pow(1 + (double)monthlyRate, n);
        var installmentAmount = principal * (monthlyRate * factor) / (factor - 1);

        var remaining = principal;

        for (int i = 1; i <= n; i++)
        {
            var interest = remaining * monthlyRate;
            var principalPart = installmentAmount - interest;

            if (i == n) // ajuste final
                principalPart = remaining;

            remaining -= principalPart;

            installments.Add(new Installment(
                i,
                terms.StartDate.AddMonths(i),
                principalPart,
                interest
            ));
        }

        return new AmortizationSchedule(installments);
    }
}