namespace CreditSystem.Domain.Enums;

public enum RevolvingCreditStatus
{
    Pending,    // Created, pending activation
    Active,     // Active, funds can be drawn
    Frozen,     // Frozen due to delinquency
    Closed      // Closed
}