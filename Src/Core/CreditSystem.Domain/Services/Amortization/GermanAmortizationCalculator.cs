using System;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Services.Amortization;

public class GermanAmortizationCalculator : IAmortizationCalculator
{
    public AmortizationMethod Method => AmortizationMethod.German;

    public PaymentSchedule Calculate(Money principal, InterestRate rate, int termMonths, DateTime startDate)
    {
        var entries = new List<AmortizationEntry>();
        var balance = principal;
        var fixedPrincipal = new Money(principal.Amount / termMonths, principal.Currency);

        for (int i = 1; i <= termMonths; i++)
        {
            var interest = rate.CalculateMonthlyInterest(balance);
            var principalPaid = fixedPrincipal;

            // Ajustar último pago
            if (i == termMonths)
            {
                principalPaid = balance;
            }

            var totalPayment = principalPaid + interest;
            balance -= principalPaid;

            entries.Add(new AmortizationEntry
            {
                PaymentNumber = i,
                DueDate = startDate.AddMonths(i),
                TotalPayment = totalPayment,
                Principal = principalPaid,
                Interest = interest,
                Balance = balance
            });
        }

        return new PaymentSchedule(entries);
    }
}
