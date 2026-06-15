using Crm.Application.Abstractions.Messaging;
using Crm.Application.RiskEngine.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.RiskEngine;

public record CreateRiskMatrixCommand(CreateRiskMatrixDto Dto) : ICommand<RiskMatrixDto>;

internal sealed class CreateRiskMatrixCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRiskMatrixCommand, RiskMatrixDto>
{
    public async Task<Result<RiskMatrixDto>> Handle(CreateRiskMatrixCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.AutoApproveThreshold <= request.Dto.AutoRejectThreshold)
            return Result.Failure<RiskMatrixDto>(RiskMatrixError.OverlappingThresholds);

        if (request.Dto.RuleIds.Count == 0)
            return Result.Failure<RiskMatrixDto>(RiskMatrixError.NoRules);

        var matrix = new RiskMatrix
        {
            Id = Guid.CreateVersion7(),
            Name = request.Dto.Name,
            Version = 1,
            Status = RiskMatrixStatus.Draft,
            AutoApproveThreshold = request.Dto.AutoApproveThreshold,
            AutoRejectThreshold = request.Dto.AutoRejectThreshold,
            CreatedAt = DateTime.UtcNow
        };

        int order = 0;
        foreach (var ruleId in request.Dto.RuleIds)
        {
            matrix.MatrixRules.Add(new RiskMatrixRule
            {
                Id = Guid.CreateVersion7(),
                RiskMatrixId = matrix.Id,
                RiskRuleId = ruleId,
                Order = order++
            });
        }

        await unitOfWork.RiskMatrixRepository.AddAsync(matrix, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RiskMatrixDto(matrix.Id, matrix.Name, matrix.Version, matrix.Status,
            matrix.AutoApproveThreshold, matrix.AutoRejectThreshold, matrix.CreatedAt);
    }
}

internal sealed class CreateRiskMatrixCommandValidator : AbstractValidator<CreateRiskMatrixCommand>
{
    public CreateRiskMatrixCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty();
        RuleFor(x => x.Dto.RuleIds).NotEmpty().WithMessage("At least one rule is required");
        RuleFor(x => x.Dto.AutoApproveThreshold).GreaterThan(x => x.Dto.AutoRejectThreshold)
            .WithMessage("AutoApproveThreshold must be greater than AutoRejectThreshold");
    }
}
