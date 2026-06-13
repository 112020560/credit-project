using System;

namespace Credit.Domain.Models;

public class PaymentModel
{
    public Guid Id { get; set; }

    public Guid? CreditLineId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; }

    public string? Method { get; set; }

    public string? Metadata { get; set; }
}
