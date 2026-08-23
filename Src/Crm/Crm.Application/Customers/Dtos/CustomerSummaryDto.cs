namespace Crm.Application.Customers.Dtos;

public record CustomerSummaryDto(
    Guid Id,
    string FullName,
    string DisplayName,
    string IdentificationNumber
    );