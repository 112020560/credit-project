namespace Crm.Domain.Documents;

public class DocumentValidation
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Decision { get; set; } = null!;
    public string? RejectionReason { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime ReviewedAt { get; set; }

    public virtual Document Document { get; set; } = null!;
}
