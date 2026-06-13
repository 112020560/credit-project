using System;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Services.Amortization;

public class FrenchAmortizationCalculator : IAmortizationCalculator
{
    public AmortizationMethod Method => AmortizationMethod.French;

    public PaymentSchedule Calculate(Money principal, InterestRate rate, int termMonths, DateTime startDate)
    {
        var entries = new List<AmortizationEntry>();
        var balance = principal;
        var monthlyPayment = CalculateMonthlyPayment(principal, rate, termMonths);

        for (int i = 1; i <= termMonths; i++)
        {
            var interest = rate.CalculateMonthlyInterest(balance);
            var principalPaid = monthlyPayment - interest;

            // Handle rounding issues: if principal to pay exceeds balance, adjust it
            if (principalPaid.Amount > balance.Amount)
            {
                principalPaid = balance;
            }

            balance = balance - principalPaid;

            // Adjust last payment to clear any remaining balance due to rounding
            if (i == termMonths && balance.Amount != 0)
            {
                principalPaid = principalPaid + balance;
                balance = Money.Zero(principal.Currency);
            }

            entries.Add(new AmortizationEntry
            {
                PaymentNumber = i,
                DueDate = startDate.AddMonths(i),
                TotalPayment = monthlyPayment,
                Principal = principalPaid,
                Interest = interest,
                Balance = balance
            });
        }

        return new PaymentSchedule(entries);
    }

    private static Money CalculateMonthlyPayment(Money principal, InterestRate rate, int months)
    {
        var r = rate.MonthlyRate;
        var n = months;
        var p = principal.Amount;

        if (r == 0)
            return new Money(p / n, principal.Currency);

        var payment = p * (r * (decimal)Math.Pow((double)(1 + r), n)) / ((decimal)Math.Pow((double)(1 + r), n) - 1);

        return new Money(payment, principal.Currency);
    }
}