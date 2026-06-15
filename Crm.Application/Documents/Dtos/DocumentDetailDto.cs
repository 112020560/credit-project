namespace Crm.Application.Documents.Dtos;

public record DocumentDetailDto(
    Guid Id,
    Guid OwnerId,
    string OwnerType,
    string DocumentTypeCode,
    string StorageUrl,
    string Status,
    DateTime UploadedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DocumentValidationDto> Validations);
