using Crm.Application.Abstractions.Messaging;
using Crm.Application.RiskEngine.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using SharedKernel;

namespace Crm.Application.RiskEngine;

public record GetRiskEvaluationByApplicationIdQuery(Guid ApplicationId) : IQuery<RiskEvaluationDto>;

internal sealed class GetRiskEvaluationByApplicationIdQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetRiskEvaluationByApplicationIdQuery, RiskEvaluationDto>
{
    public async Task<Result<RiskEvaluationDto>> Handle(GetRiskEvaluationByApplicationIdQuery request, CancellationToken cancellationToken)
    {
        var evaluations = await unitOfWork.RiskEvaluationsRepository.GetByCreditApplicationIdAsync(request.ApplicationId, cancellationToken);
        var latest = evaluations.OrderByDescending(e => e.EvaluatedAt).FirstOrDefault();
        if (latest is null)
            return Result.Failure<RiskEvaluationDto>(RiskEvaluationError.NotFound(request.ApplicationId));

        return TriggerRiskEvaluationCommandHandler.ToDto(latest);
    }
}
