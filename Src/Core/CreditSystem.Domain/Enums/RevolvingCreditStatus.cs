namespace CreditSystem.Domain.Enums;

public enum RevolvingCreditStatus
{
    Pending,    // Creada, pendiente de activación
    Active,     // Activa, puede disponer fondos
    Frozen,     // Congelada por mora
    Closed      // Cerrada
}