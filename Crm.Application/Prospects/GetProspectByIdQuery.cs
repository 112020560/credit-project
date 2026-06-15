using Crm.Application.Abstractions.Messaging;
using Crm.Application.Prospects.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Prospects;
using SharedKernel;

namespace Crm.Application.Prospects;

public record GetProspectByIdQuery(Guid ProspectId) : IQuery<ProspectSummaryDto>;

internal sealed class GetProspectByIdQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetProspectByIdQuery, ProspectSummaryDto>
{
    public async Task<Result<ProspectSummaryDto>> Handle(GetProspectByIdQuery request, CancellationToken cancellationToken)
    {
        var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(request.ProspectId, cancellationToken);
        if (prospect is null)
            return Result.Failure<ProspectSummaryDto>(ProspectError.NotFound(request.ProspectId));

        return new ProspectSummaryDto(prospect.Id, prospect.FullName, prospect.DisplayName, prospect.IdentificationNumber, prospect.Status);
    }
}
