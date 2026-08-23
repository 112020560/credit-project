using Crm.Application.Abstractions.Messaging;
using Crm.Application.CreditApplications.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using SharedKernel;

namespace Crm.Application.CreditApplications;

public record GetCreditApplicationByIdQuery(Guid ApplicationId) : IQuery<CreditApplicationDetailDto>;

internal sealed class GetCreditApplicationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetCreditApplicationByIdQuery, CreditApplicationDetailDto>
{
    public async Task<Result<CreditApplicationDetailDto>> Handle(GetCreditApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure<CreditApplicationDetailDto>(CreditApplicationError.NotFound(request.ApplicationId));

        return CreateCreditApplicationCommandHandler.ToDto(application);
    }
}
