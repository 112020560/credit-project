using SharedKernel.Contracts.Crm.CreditApplications;

namespace Crm.Application.CreditApplications.Dtos;

public class CreditApplicationSubmittedContract : CreditApplicationSubmitted
{
    public Guid ApplicationId { get; set; }
    public Guid ProspectId { get; set; }
}
