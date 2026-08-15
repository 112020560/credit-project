using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Models;

/// <summary>
/// Evento de notificación emitido cuando la categoría de riesgo de un préstamo cambia.
/// No es un evento del aggregate — no se persiste en el event store.
/// </summary>
public record LoanRiskCategoryChanged
{
    public Guid LoanId { get; init; }
    public LoanRiskCategory? PreviousCategory { get; init; }
    public LoanRiskCategory NewCategory { get; init; }
    public int DaysOverdue { get; init; }
    public decimal EstimatedProvision { get; init; }
    public bool IsManual { get; init; }
    public DateTime ClassifiedAt { get; init; }
}
