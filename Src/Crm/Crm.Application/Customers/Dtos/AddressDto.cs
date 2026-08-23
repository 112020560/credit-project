namespace Crm.Application.Customers.Dtos;

public record AddressDto(
    string Type,
    string Country,
    string State,
    string City,
    string Street,
    string PostalCode,
    bool IsPrimary = false
);