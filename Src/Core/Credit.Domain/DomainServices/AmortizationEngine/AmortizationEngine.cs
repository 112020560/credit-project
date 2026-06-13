

namespace Credit.Domain.DomainServices.AmortizationEngine;

public sealed class AmortizationEngine
{
    private readonly Dictionary<CreditType, IAmortizationStrategy> _strategies;

    public AmortizationEngine(IEnumerable<IAmortizationStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.CreditType);
    }

    public AmortizationSchedule CalculateSchedule(CreditTerms terms)
    {
        if (!_strategies.TryGetValue(terms.Type, out var strategy))
            throw new DomainException($"No amortization strategy for credit type {terms.Type}");

        return strategy.Calculate(terms);
    }
}
