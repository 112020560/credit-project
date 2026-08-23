using System;
using SharedKernel.Contracts.Crm.Customers;

namespace Crm.Application.Customers.Dtos;

public class CreateCustomerContract : CustomerCreated
{
    public Guid CustomerId { get; set; }

    public string FullName { get; set; }

    public string DisplayName { get; set; }

    public string IdentificationType { get; set; } 

    public string IdentificationNumber { get; set; }

    public string? TaxId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Version { get; set; }

    public IDictionary<string, object>? Metadata { get; set; }
}
