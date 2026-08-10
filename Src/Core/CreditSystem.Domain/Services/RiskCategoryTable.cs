using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Services;

/// <summary>
/// Tabla de categorías de riesgo según SUGEF 1-05. Inmutable — valores definidos por regulación.
/// </summary>
public static class RiskCategoryTable
{
    public static LoanRiskCategory GetCategory(int daysOverdue) => daysOverdue switch
    {
        0           => LoanRiskCategory.A1,
        <= 30       => LoanRiskCategory.A2,
        <= 60       => LoanRiskCategory.B1,
        <= 90       => LoanRiskCategory.B2,
        <= 120      => LoanRiskCategory.C1,
        <= 180      => LoanRiskCategory.C2,
        <= 360      => LoanRiskCategory.D,
        _           => LoanRiskCategory.E
    };

    public static decimal GetProvisionRate(LoanRiskCategory category) => category switch
    {
        LoanRiskCategory.A1 => 0.000m,
        LoanRiskCategory.A2 => 0.005m,
        LoanRiskCategory.B1 => 0.05m,
        LoanRiskCategory.B2 => 0.10m,
        LoanRiskCategory.C1 => 0.25m,
        LoanRiskCategory.C2 => 0.50m,
        LoanRiskCategory.D  => 0.75m,
        LoanRiskCategory.E  => 1.00m,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    /// <summary>
    /// Ordinal de severidad — mayor número = mayor riesgo.
    /// </summary>
    public static int Severity(LoanRiskCategory category) => (int)category;
}
