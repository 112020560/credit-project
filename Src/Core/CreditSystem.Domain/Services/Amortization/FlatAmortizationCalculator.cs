using System;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Services.Amortization;

public class FlatAmortizationCalculator : IAmortizationCalculator
{
    public AmortizationMethod Method => AmortizationMethod.Flat;

    public PaymentSchedule Calculate(Money principal, InterestRate rate, int termMonths, DateTime startDate)
    {
        var entries = new List<AmortizationEntry>();
        
        // Interés flat: se calcula sobre el capital inicial, no sobre saldo
        var totalInterest = new Money(principal.Amount * (rate.AnnualRate / 100) * (termMonths / 12m), principal.Currency);
        var monthlyInterest = new Money(totalInterest.Amount / termMonths, principal.Currency);
        var monthlyPrincipal = new Money(principal.Amount / termMonths, principal.Currency);
        var monthlyPayment = monthlyPrincipal + monthlyInterest;
        var balance = principal;

        for (int i = 1; i <= termMonths; i++)
        {
            var principalPaid = monthlyPrincipal;

            // Ajustar último pago
            if (i == termMonths)
            {
                principalPaid = balance;
            }

            balance -= principalPaid;

            entries.Add(new AmortizationEntry
            {
                PaymentNumber = i,
                DueDate = startDate.AddMonths(i),
                TotalPayment = monthlyPayment,
                Principal = principalPaid,
                Interest = monthlyInterest,
                Balance = balance
            });
        }

        return new PaymentSchedule(entries);
    }
}
