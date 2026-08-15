using System;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Services.Amortization;

public class AmericanAmortizationCalculator : IAmortizationCalculator
{
    public AmortizationMethod Method => AmortizationMethod.American;

    public PaymentSchedule Calculate(Money principal, InterestRate rate, int termMonths, DateTime startDate)
    {
        var entries = new List<AmortizationEntry>();
        var monthlyInterest = rate.CalculateMonthlyInterest(principal);

        for (int i = 1; i <= termMonths; i++)
        {
            var isLastPayment = i == termMonths;
            var principalPaid = isLastPayment ? principal : Money.Zero(principal.Currency);
            var totalPayment = isLastPayment ? principal + monthlyInterest : monthlyInterest;
            var balance = isLastPayment ? Money.Zero(principal.Currency) : principal;

            entries.Add(new AmortizationEntry
            {
                PaymentNumber = i,
                DueDate = startDate.AddMonths(i),
                TotalPayment = totalPayment,
                Principal = principalPaid,
                Interest = monthlyInterest,
                Balance = balance
            });
        }

        return new PaymentSchedule(entries);
    }
}