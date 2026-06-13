using System;

namespace Credit.Domain;

public sealed class Installment
{
    public int Number { get; }
    public DateOnly DueDate { get; }
    public decimal PrincipalDue { get; }   // outstanding principal portion for this installment
    public decimal InterestDue { get; }    // interest portion
    public decimal TotalDue => decimal.Round(PrincipalDue + InterestDue, 2);
    public decimal PrincipalPaid { get; private set; }
    public decimal InterestPaid { get; private set; }

    public bool IsPaid => PrincipalPaid >= PrincipalDue && InterestPaid >= InterestDue;

    public Installment(int number, DateOnly dueDate, decimal principalDue, decimal interestDue)
    {
        Number = number;
        DueDate = dueDate;
        PrincipalDue = decimal.Round(principalDue, 2);
        InterestDue = decimal.Round(interestDue, 2);
        PrincipalPaid = 0m;
        InterestPaid = 0m;
    }

    public (decimal principalApplied, decimal interestApplied) ApplyPayment(decimal amount)
    {
        // Apply to interest first, then principal (typical)
        var remaining = amount;
        var interestNeeded = InterestDue - InterestPaid;
        var interestApplied = Math.Min(interestNeeded, remaining);
        InterestPaid += interestApplied;
        remaining -= interestApplied;

        var principalNeeded = PrincipalDue - PrincipalPaid;
        var principalApplied = Math.Min(principalNeeded, remaining);
        PrincipalPaid += principalApplied;
        remaining -= principalApplied;

        return (principalApplied, interestApplied);
    }
}

