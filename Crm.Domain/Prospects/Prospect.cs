using Crm.Domain.Prospects.Events;
using Crm.Domain.ValueObjects;
using SharedKernel;

namespace Crm.Domain.Prospects;

public class Prospect : AggregateRoot
{
    public Guid Id { get; set; }
    public string IdentificationType { get; set; } = null!;
    public string IdentificationNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string Status { get; set; } = ProspectStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    public virtual ICollection<PhoneContact> Phones { get; set; } = new List<PhoneContact>();
    public virtual ICollection<EmailContact> Emails { get; set; } = new List<EmailContact>();
    public virtual ICollection<WorkInfo> WorkInfos { get; set; } = new List<WorkInfo>();
    public virtual ICollection<FiscalInfo> FiscalInfos { get; set; } = new List<FiscalInfo>();

    public Result Submit()
    {
        if (Status != ProspectStatus.Draft)
            return Result.Failure(ProspectError.InvalidTransition(Status, ProspectStatus.Submitted));

        Status = ProspectStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProspectSubmittedEvent(Id));
        return Result.Success();
    }

    public Result Convert()
    {
        if (Status != ProspectStatus.Submitted)
            return Result.Failure(ProspectError.InvalidTransition(Status, ProspectStatus.Converted));

        Status = ProspectStatus.Converted;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProspectConvertedEvent(Id));
        return Result.Success();
    }

    public Result Reject()
    {
        if (Status != ProspectStatus.Submitted)
            return Result.Failure(ProspectError.InvalidTransition(Status, ProspectStatus.Draft));

        Status = ProspectStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProspectRejectedEvent(Id));
        return Result.Success();
    }
}
