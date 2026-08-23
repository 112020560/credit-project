namespace Crm.Application.Customers.Dtos;

public record CustomerUpdatedContract(
    Guid CustomerId,
    DateTimeOffset UpdatedAt,
    int Version,
    IDictionary<string, object> Changes);
