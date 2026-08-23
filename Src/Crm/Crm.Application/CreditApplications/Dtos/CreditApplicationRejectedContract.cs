using SharedKernel.Contracts.Crm.CreditApplications;

namespace Crm.Application.CreditApplications.Dtos;

public class CreditApplicationRejectedContract : CreditApplicationRejected
{
    public Guid ApplicationId { get; set; }
    public Guid ProspectId { get; set; }
    public string? RejectionReason { get; set; }
}
