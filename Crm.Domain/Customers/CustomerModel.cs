using System;

namespace Crm.Domain.Customers;

public class CustomerModel
{
    public Guid Id { get; set; }

    public string? ExternalCode { get; set; }

    public string? IdentificationType { get; set; }

    public string? IdentificationNumber { get; set; }

    public string FullName { get; set; } = null!;

    public string? DisplayName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
