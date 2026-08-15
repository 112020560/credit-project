using System;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;

namespace CreditSystem.Domain.Services.Amortization;

public class AmortizationCalculatorFactory : IAmortizationCalculatorFactory
{
    private readonly Dictionary<AmortizationMethod, IAmortizationCalculator> _calculators;

    public AmortizationCalculatorFactory(IEnumerable<IAmortizationCalculator> calculators)
    {
        _calculators = calculators.ToDictionary(c => c.Method);
    }

    public IAmortizationCalculator GetCalculator(AmortizationMethod method)
    {
        if (!_calculators.TryGetValue(method, out var calculator))
        {
            throw new DomainException($"Amortization method {method} is not supported");
        }

        return calculator;
    }
}
