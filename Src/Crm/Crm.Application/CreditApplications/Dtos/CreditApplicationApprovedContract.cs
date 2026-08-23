using SharedKernel.Contracts.Crm.CreditApplications;

namespace Crm.Application.CreditApplications.Dtos;

public class CreditApplicationApprovedContract : CreditApplicationApproved
{
    public Guid ApplicationId { get; set; }
    public Guid ProspectId { get; set; }
    public string Status { get; set; } = null!;
}
