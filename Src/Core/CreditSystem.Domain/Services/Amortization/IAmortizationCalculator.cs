using System;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Services.Amortization;

public interface IAmortizationCalculator
{
    AmortizationMethod Method { get; }
    PaymentSchedule Calculate(Money principal, InterestRate rate, int termMonths, DateTime startDate);
}
