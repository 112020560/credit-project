namespace Crm.Application.Documents.Dtos;

public record DocumentRejectedContract(
    Guid DocumentId,
    Guid OwnerId,
    string OwnerType,
    string? RejectionReason,
    string? ReviewedBy,
    DateTime ReviewedAt);
