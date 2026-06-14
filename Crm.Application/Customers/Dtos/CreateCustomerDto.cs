namespace Crm.Application.Customers.Dtos;

public record CreateCustomerDto
(
    string IdentificationType,
    string IdentificationNumber,
    string FullName,
    string? DisplayName,
    string? TaxId,
    DateOnly? BirthDate,
    IEnumerable<AddressDto>? Addresses,
    IEnumerable<ContactDto>? Contacts,
    IEnumerable<CustomerWorkInfoDto>? WorkInfos);