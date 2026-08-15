namespace Crm.Domain.ValueObjects;

public record PhoneContact(
    string Number,
    string? Type,
    string? CountryCode,
    bool? IsPrimary,
    bool? Verified);
