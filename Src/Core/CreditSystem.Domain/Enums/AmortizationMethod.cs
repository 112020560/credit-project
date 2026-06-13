namespace CreditSystem.Domain.Enums;

public enum AmortizationMethod
{
    French,         // Cuota fija (actual)
    German,         // Capital fijo, cuota decreciente
    American,       // Solo intereses + bullet al final
    Flat,           // Interés sobre capital inicial
    InterestOnly    // Solo intereses, capital al final
}