using SharedKernel.Contracts.Crm.Prospects;

namespace Crm.Application.Prospects.Dtos;

public class ProspectConvertedContract : ProspectConverted
{
    public Guid ProspectId { get; set; }
    public Guid CustomerId { get; set; }
}
