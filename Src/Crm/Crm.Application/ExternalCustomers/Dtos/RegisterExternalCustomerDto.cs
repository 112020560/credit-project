namespace Crm.Application.ExternalCustomers.Dtos;

public record RegisterExternalCustomerDto(
    Guid ExternalId,
    string? DisplayName,
    string? LegalName,
    decimal? RiskScore,
    string? Metadata);
