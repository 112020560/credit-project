using Crm.Application.Abstractions.Messaging;
using Crm.Application.RiskEngine.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using SharedKernel;

namespace Crm.Application.RiskEngine;

public record GetRiskEvaluationByIdQuery(Guid EvaluationId) : IQuery<RiskEvaluationDto>;

internal sealed class GetRiskEvaluationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<GetRiskEvaluationByIdQuery, RiskEvaluationDto>
{
    public async Task<Result<RiskEvaluationDto>> Handle(GetRiskEvaluationByIdQuery request, CancellationToken cancellationToken)
    {
        var evaluation = await unitOfWork.RiskEvaluationsRepository.GetByIdAsync(request.EvaluationId, cancellationToken);
        if (evaluation is null)
            return Result.Failure<RiskEvaluationDto>(RiskEvaluationError.NotFound(request.EvaluationId));

        return TriggerRiskEvaluationCommandHandler.ToDto(evaluation);
    }
}
