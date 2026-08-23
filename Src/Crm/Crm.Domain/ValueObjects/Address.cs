namespace Crm.Domain.ValueObjects;

public record Address(
    string? Type,
    string? Street,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    bool? IsPrimary,
    string? District = null,
    string? Metadata = null);
