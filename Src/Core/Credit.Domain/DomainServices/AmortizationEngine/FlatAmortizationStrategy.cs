using System;

namespace Credit.Domain.DomainServices.AmortizationEngine;

public sealed class FlatAmortizationStrategy : IAmortizationStrategy
{
    public CreditType CreditType => CreditType.Flat;

    public AmortizationSchedule Calculate(CreditTerms terms)
    {
        var installments = new List<Installment>();

        var totalInterest = terms.Principal * terms.AnnualInterestRate * (terms.NumberOfInstallments / 12m);
        var interestPerInstallment = totalInterest / terms.NumberOfInstallments;
        var principalPerInstallment = terms.Principal / terms.NumberOfInstallments;

        for (int i = 1; i <= terms.NumberOfInstallments; i++)
        {
            installments.Add(new Installment(
                i,
                terms.StartDate.AddMonths(i),
                principalPerInstallment,
                interestPerInstallment
            ));
        }

        return new AmortizationSchedule(installments);
    }
}
