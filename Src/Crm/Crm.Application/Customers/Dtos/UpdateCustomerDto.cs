namespace Crm.Application.Customers.Dtos;

public record UpdateCustomerDto(
    string FullName,
    string? DisplayName,
    string? IdentificationType,
    string? IdentificationNumber,
    DateOnly? BirthDate);
