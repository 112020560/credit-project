using System;

namespace Credit.Domain;

public sealed class CreditTerms
{
    public decimal Principal { get; }
    public decimal AnnualInterestRate { get; } // e.g., 0.12 for 12%
    public int NumberOfInstallments { get; }   // e.g., 12, 24
    public DateOnly StartDate { get; }
    public CreditType Type { get; } // e.g., French, Flat, Revolving

    public CreditTerms(decimal principal, decimal annualInterestRate, int numberOfInstallments, DateOnly startDate, CreditType type)
    {
        if (principal <= 0) throw new ArgumentException("Principal must be > 0", nameof(principal));
        if (annualInterestRate < 0) throw new ArgumentException("Rate must be >= 0", nameof(annualInterestRate));
        if (numberOfInstallments <= 0) throw new ArgumentException("Installments must be > 0", nameof(numberOfInstallments));

        Principal = decimal.Round(principal, 2);
        AnnualInterestRate = annualInterestRate;
        NumberOfInstallments = numberOfInstallments;
        StartDate = startDate;
        Type = type;
    }
}

public enum CreditType
{
    French, // cuota fija (annuity)
    German, // cuota decreciente
    Flat,   // intereses flat
    Revolving
}
