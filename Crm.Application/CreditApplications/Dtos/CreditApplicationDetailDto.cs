namespace Crm.Application.CreditApplications.Dtos;

public record ApplicationDocumentDto(Guid Id, string Type, string StorageUrl, string Status, DateTime UploadedAt);

public record CreditApplicationDetailDto(
    Guid Id,
    Guid ProspectId,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ApplicationDocumentDto> Documents);
