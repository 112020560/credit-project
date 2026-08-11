namespace CreditSystem.Domain.Models.Documents;

public class AmortizationRowData
{
    public int EntryNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Payment { get; init; }
    public decimal Interest { get; init; }
    public decimal Principal { get; init; }
    public decimal RemainingBalance { get; init; }
    public string Currency { get; init; } = null!;
}
