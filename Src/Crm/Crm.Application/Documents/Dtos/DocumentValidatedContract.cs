namespace Crm.Application.Documents.Dtos;

public record DocumentValidatedContract(
    Guid DocumentId,
    Guid OwnerId,
    string OwnerType,
    string? ReviewedBy,
    DateTime ReviewedAt);
