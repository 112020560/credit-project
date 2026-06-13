namespace Credit.Domain.Entities;

public partial class CreditPayment
{
    public Guid Id { get; set; }

    public Guid? CreditLineId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public string? Method { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CreditLine? CreditLine { get; set; }
}
