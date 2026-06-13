using System.Runtime.InteropServices;
using Credit.Domain.Models;

namespace Credit.Domain.Entities;

public partial class CreditApplication
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

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CreditLine> CreditLines { get; set; } = new List<CreditLine>();

    public virtual Customer? Customer { get; set; }

    public virtual CreditProduct? Product { get; set; }

    public  CreditApplicationModel MapToModel()
    {
        return new CreditApplicationModel
        {
            Id = this.Id,
            CustomerId = this.CustomerId,
            ProductId = this.ProductId,
            Amount = this.Amount,
            TermMonths = this.TermMonths,
            Status = this.Status,
            Score = this.Score,
            DecisionNotes = this.DecisionNotes,
            Documents = this.Documents
        };
    }
}
