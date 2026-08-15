using Crm.Domain.Customers.Events;
using Crm.Domain.ValueObjects;
using SharedKernel;

namespace Crm.Domain.Customers;

public class Customer : AggregateRoot
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

    public decimal? CreditScore { get; set; }

    public decimal? MonthlyIncome { get; set; }

    public decimal? MonthlyDebt { get; set; }

    public virtual ICollection<Address> CustomerAddresses { get; set; } = new List<Address>();

    public virtual ICollection<CustomerDocument> CustomerDocuments { get; set; } = new List<CustomerDocument>();

    public virtual ICollection<EmailContact> CustomerEmails { get; set; } = new List<EmailContact>();

    public virtual ICollection<FiscalInfo> CustomerFiscalInfos { get; set; } = new List<FiscalInfo>();

    public virtual ICollection<PhoneContact> CustomerPhones { get; set; } = new List<PhoneContact>();

    public virtual ICollection<WorkInfo> CustomerWorkInfos { get; set; } = new List<WorkInfo>();

    public Result Activate()
    {
        Status = CustomerStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerActivatedEvent(Id));
        return Result.Success();
    }

    public Result Deactivate()
    {
        Status = CustomerStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerDeactivatedEvent(Id));
        return Result.Success();
    }

    public Result UpdateProfile(string fullName, string? displayName, string? identificationType, string? identificationNumber, DateOnly? birthDate)
    {
        FullName = fullName;
        DisplayName = displayName;
        IdentificationType = identificationType;
        IdentificationNumber = identificationNumber;
        BirthDate = birthDate;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerProfileUpdatedEvent(Id));
        return Result.Success();
    }

    public Result UpdateFinancials(decimal? creditScore, decimal? monthlyIncome, decimal? monthlyDebt)
    {
        if (creditScore is not null) CreditScore = creditScore;
        if (monthlyIncome is not null) MonthlyIncome = monthlyIncome;
        if (monthlyDebt is not null) MonthlyDebt = monthlyDebt;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerFinancialsUpdatedEvent(Id));
        return Result.Success();
    }

    public static Customer FromProspect(Prospects.Prospect prospect)
    {
        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            IdentificationType = prospect.IdentificationType,
            IdentificationNumber = prospect.IdentificationNumber,
            FullName = prospect.FullName,
            DisplayName = prospect.DisplayName,
            BirthDate = prospect.BirthDate,
            Status = CustomerStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CustomerEmails = [.. prospect.Emails],
            CustomerPhones = [.. prospect.Phones],
            CustomerAddresses = [.. prospect.Addresses],
            CustomerWorkInfos = [.. prospect.WorkInfos],
            CustomerFiscalInfos = [.. prospect.FiscalInfos],
        };
        customer.RaiseDomainEvent(new CustomerCreatedEvent(customer.Id));
        return customer;
    }
}
