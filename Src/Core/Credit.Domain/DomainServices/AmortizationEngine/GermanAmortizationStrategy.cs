using System;

namespace Credit.Domain.DomainServices.AmortizationEngine;

public sealed class GermanAmortizationStrategy : IAmortizationStrategy
{
    public CreditType CreditType => CreditType.German;

    public AmortizationSchedule Calculate(CreditTerms terms)
    {
        var installments = new List<Installment>();

        var monthlyRate = terms.AnnualInterestRate / 12m;
        var principalPart = terms.Principal / terms.NumberOfInstallments;
        var remaining = terms.Principal;

        for (int i = 1; i <= terms.NumberOfInstallments; i++)
        {
            var interest = remaining * monthlyRate;

            if (i == terms.NumberOfInstallments)
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
