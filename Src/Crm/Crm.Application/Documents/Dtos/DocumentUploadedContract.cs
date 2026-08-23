namespace Crm.Application.Documents.Dtos;

public record DocumentUploadedContract(
    Guid DocumentId,
    Guid OwnerId,
    string OwnerType,
    string DocumentTypeCode,
    string StorageUrl,
    DateTime UploadedAt);
