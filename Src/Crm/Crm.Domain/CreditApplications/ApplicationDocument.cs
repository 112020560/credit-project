namespace Crm.Domain.CreditApplications;

public class ApplicationDocument
{
    public Guid Id { get; set; }
    public Guid CreditApplicationId { get; set; }
    public string Type { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public string Status { get; set; } = ApplicationDocumentStatus.Uploaded;
    public DateTime UploadedAt { get; set; }

    public virtual CreditApplication CreditApplication { get; set; } = null!;
}
