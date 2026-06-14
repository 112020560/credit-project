namespace Crm.Application.Customers.Dtos;

public record ContactDto(string Type, string Value, bool IsPrimary = false);