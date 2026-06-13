using System;
using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Services.Amortization;

public interface IAmortizationCalculatorFactory
{
    IAmortizationCalculator GetCalculator(AmortizationMethod method);
}
