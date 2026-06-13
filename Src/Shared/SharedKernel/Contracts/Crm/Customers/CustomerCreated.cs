namespace SharedKernel.Contracts.Crm.Customers;

public interface CustomerCreated
{
    Guid CustomerId { get; }
    string FullName { get; }
    string DisplayName { get; }
    public string IdentificationType { get; set; }
    string IdentificationNumber { get; }
    string? TaxId { get; }
    string? Email { get; }
    string? Phone { get; }
    DateTimeOffset CreatedAt { get; }
    int Version { get; } // optional version for idempotency
    IDictionary<string, object>? Metadata { get; }
}