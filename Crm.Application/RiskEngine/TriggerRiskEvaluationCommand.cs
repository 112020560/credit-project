using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.RiskEngine.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using Crm.Domain.RiskEngine;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.RiskEngine;

public record TriggerRiskEvaluationCommand(TriggerRiskEvaluationDto Dto) : ICommand<RiskEvaluationDto>;

internal sealed class TriggerRiskEvaluationCommandHandler(
    IUnitOfWork unitOfWork,
    RiskEvaluationService riskEvaluationService,
    IMqProducerService mqProducerService)
    : ICommandHandler<TriggerRiskEvaluationCommand, RiskEvaluationDto>
{
    public async Task<Result<RiskEvaluationDto>> Handle(TriggerRiskEvaluationCommand request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.Dto.CreditApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure<RiskEvaluationDto>(CreditApplicationError.NotFound(request.Dto.CreditApplicationId));

        if (application.Status != CreditApplicationStatus.InReview)
            return Result.Failure<RiskEvaluationDto>(RiskEvaluationError.ApplicationNotEligible);

        var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(application.ProspectId, cancellationToken);
        if (prospect is null)
            return Result.Failure<RiskEvaluationDto>(RiskEvaluationError.NoActiveMatrix);

        var (evaluation, hasMatrix) = await riskEvaluationService.EvaluateAsync(application, prospect, cancellationToken);
        if (!hasMatrix)
            return Result.Failure<RiskEvaluationDto>(RiskEvaluationError.NoActiveMatrix);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");
        await mqProducerService.PublishEvent(new
        {
            evaluation.Id,
            evaluation.CreditApplicationId,
            evaluation.RiskMatrixId,
            evaluation.RiskMatrixVersion,
            StartedAt = evaluation.EvaluatedAt
        }, traceId, cancellationToken);
        await mqProducerService.PublishEvent(new
        {
            evaluation.Id,
            evaluation.CreditApplicationId,
            evaluation.RiskMatrixId,
            evaluation.RiskMatrixVersion,
            evaluation.TotalScore,
            evaluation.Outcome,
            CompletedAt = evaluation.EvaluatedAt
        }, traceId, cancellationToken);

        return ToDto(evaluation);
    }

    internal static RiskEvaluationDto ToDto(Domain.RiskEngine.RiskEvaluation e) => new(
        e.Id, e.CreditApplicationId, e.RiskMatrixId, e.RiskMatrixVersion,
        e.TotalScore, e.Outcome, e.SuggestedInterestRate, e.SuggestedMaxAmount, e.EvaluatedAt,
        e.Entries.Select(s => new ScoreCardEntryDto(s.RuleId, s.RuleName, s.TargetField, s.ObservedValue, s.Passed, s.WeightedContribution)).ToList());
}

internal sealed class TriggerRiskEvaluationCommandValidator : AbstractValidator<TriggerRiskEvaluationCommand>
{
    public TriggerRiskEvaluationCommandValidator()
    {
        RuleFor(x => x.Dto.CreditApplicationId).NotEmpty();
    }
}
