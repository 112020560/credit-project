using System;
using System.Collections.Generic;
using System.Linq;
using Credit.Domain;

namespace Credit.Domain;

public sealed class AmortizationSchedule
{
    public IReadOnlyList<Installment> Installments { get; }

    public AmortizationSchedule(IEnumerable<Installment> installments)
    {
        Installments = installments.ToList().AsReadOnly();
        if (!Installments.Any()) throw new ArgumentException("Schedule must contain at least one installment.");
    }

    public Installment? GetNextDueInstallment()
    {
        return Installments.FirstOrDefault(i => !i.IsPaid);
    }

    public decimal OutstandingPrincipal()
    {
        return Installments.Sum(i => Math.Max(0, i.PrincipalDue - i.PrincipalPaid));
    }
}
