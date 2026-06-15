namespace Crm.Domain.Documents;

public class Document
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerType { get; set; } = null!;
    public string DocumentTypeCode { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public string Status { get; set; } = DocumentStatus.Uploaded;
    public DateTime UploadedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<DocumentValidation> Validations { get; set; } = new List<DocumentValidation>();
}
