using System;

namespace Credit.Domain.Models;

public class InstallmentModel
{
    public Guid Id { get; set; }

    public Guid? CreditLineId { get; set; }
    public int QuotaNumber { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal PrincipalDue { get; set; }

    public decimal InterestDue { get; set; }

    public decimal? FeesDue { get; set; }

    public string Status { get; set; } = null!;
}
