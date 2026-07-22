namespace CreditSystem.Domain.Enums;

public enum AmortizationMethod
{
    French,         // Fixed installment (equal payments)
    German,         // Fixed principal, decreasing installment
    American,       // Interest only + bullet at maturity
    Flat,           // Interest calculated on original principal
    InterestOnly    // Interest only, principal at maturity
}