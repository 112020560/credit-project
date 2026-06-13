using System;

namespace Credit.Domain.DomainServices.AmortizationEngine;

public sealed class RevolvingAmortizationStrategy : IAmortizationStrategy
{
    public CreditType CreditType => CreditType.Revolving;

    public AmortizationSchedule Calculate(CreditTerms terms)
    {
        // Placeholder:
        // En revolving no hay schedule fijo inicial
        return new AmortizationSchedule(new List<Installment>());
    }
}
