namespace Crm.Application.Documents.Dtos;

public record DocumentValidationDto(Guid Id, string Decision, string? RejectionReason, string? ReviewedBy, DateTime ReviewedAt);
