namespace Crm.Domain.ValueObjects;

public record EmailContact(
    string Email,
    bool? IsPrimary,
    bool? Verified);
