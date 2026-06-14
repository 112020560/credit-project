namespace Crm.Domain.Customers;

public partial class Customer
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

    public virtual ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();

    public virtual ICollection<CustomerDocument> CustomerDocuments { get; set; } = new List<CustomerDocument>();

    public virtual ICollection<CustomerEmail> CustomerEmails { get; set; } = new List<CustomerEmail>();

    public virtual ICollection<CustomerFiscalInfo> CustomerFiscalInfos { get; set; } = new List<CustomerFiscalInfo>();

    public virtual ICollection<CustomerPhone> CustomerPhones { get; set; } = new List<CustomerPhone>();

    public virtual ICollection<CustomerWorkInfo> CustomerWorkInfos { get; set; } = new List<CustomerWorkInfo>();

    public CustomerModel ConvertToModel()
    {
        return new CustomerModel
        {
            Id = this.Id,
            ExternalCode = this.ExternalCode,
            IdentificationType = this.IdentificationType,
            IdentificationNumber = this.IdentificationNumber,
            FullName = this.FullName,
            DisplayName = this.DisplayName,
            BirthDate = this.BirthDate,
            Status = this.Status,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
        };
    }
}
