using System.Text.Json.Serialization;
using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.ValueObjects;

public record PaymentSchedule
{
    public IReadOnlyList<AmortizationEntry> Entries { get; }
    public Money TotalInterest { get; }
    public Money TotalPayment { get; }
    public AmortizationMethod Method { get; init; }

    // Constructor sin parámetros para deserialización
    [JsonConstructor]
    public PaymentSchedule()
    {
        Entries = Array.Empty<AmortizationEntry>();
        TotalInterest = Money.Zero();
        TotalPayment = Money.Zero();
        Method = AmortizationMethod.French;
    }

    public PaymentSchedule(IEnumerable<AmortizationEntry> entries, AmortizationMethod method = AmortizationMethod.French)
    {
        Entries = entries.ToList().AsReadOnly();
        TotalInterest = new Money(Entries.Sum(e => e.Interest.Amount));
        TotalPayment = new Money(Entries.Sum(e => e.TotalPayment.Amount));
        Method = method;
    }

    public AmortizationEntry? GetCurrentPayment(DateTime asOf)
        => Entries.FirstOrDefault(e => e.DueDate >= asOf && e.Balance.Amount > 0);

    public static PaymentSchedule Calculate(
        Money principal, 
        InterestRate rate, 
        int termMonths, 
        DateTime startDate)
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

            // Ajustar último pago
            if (i == termMonths && balance.Amount != 0)
            {
                principalPaid = principalPaid.Add(balance);
                balance = Money.Zero();
            }

            entries.Add(new AmortizationEntry(
                paymentNumber: i,
                dueDate: startDate.AddMonths(i),
                totalPayment: monthlyPayment,
                principal: principalPaid,
                interest: interest,
                balance: balance
            ));
        }

        return new PaymentSchedule(entries);
    }

    private static Money CalculateMonthlyPayment(Money principal, InterestRate rate, int months)
    {
        var r = rate.MonthlyRate;
        var n = months;
        var p = principal.Amount;

        // Fórmula de amortización: P * [r(1+r)^n] / [(1+r)^n - 1]
        var powerPart = (decimal)Math.Pow((double)(1 + r), (double)n);
        var payment = p * (r * powerPart) / (powerPart - 1);
        
        return new Money((decimal)payment, principal.Currency);
    }
}