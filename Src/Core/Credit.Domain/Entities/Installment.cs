namespace Credit.Domain.Entities;

public partial class Installment
{
    public Guid Id { get; set; }

    public Guid? CreditLineId { get; set; }
    public int QuotaNumber { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal PrincipalDue { get; set; }

    public decimal InterestDue { get; set; }

    public decimal? FeesDue { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CreditLine? CreditLine { get; set; }
}
