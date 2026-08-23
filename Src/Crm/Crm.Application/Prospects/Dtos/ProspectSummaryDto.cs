namespace Crm.Application.Prospects.Dtos;

public record ProspectSummaryDto(Guid Id, string FullName, string? DisplayName, string IdentificationNumber, string Status);
