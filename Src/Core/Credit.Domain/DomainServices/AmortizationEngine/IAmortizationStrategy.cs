
namespace Credit.Domain.DomainServices.AmortizationEngine;

public interface IAmortizationStrategy
{
    CreditType CreditType { get; }

    AmortizationSchedule Calculate(CreditTerms terms);
}
