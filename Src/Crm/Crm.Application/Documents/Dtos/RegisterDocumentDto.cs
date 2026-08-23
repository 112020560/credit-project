namespace Crm.Application.Documents.Dtos;

public record RegisterDocumentDto(Guid OwnerId, string OwnerType, string DocumentTypeCode, string StorageUrl);
