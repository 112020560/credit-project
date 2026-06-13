using System;

namespace Credit.Domain.Models;

public class CreditApplicationModel
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? ProductId { get; set; }

    public decimal Amount { get; set; }

    public int TermMonths { get; set; }

    public string Status { get; set; } = null!;

    public string? Score { get; set; }

    public string? DecisionNotes { get; set; }

    public string? Documents { get; set; }
}
