namespace SharedKernel.Contracts.Crm.Customers;

public interface CustomerUpdated
{
    Guid CustomerId { get; }
    DateTimeOffset UpdatedAt { get; }
    int Version { get; }
    IDictionary<string, object>? Changes { get; } // field -> new value
}