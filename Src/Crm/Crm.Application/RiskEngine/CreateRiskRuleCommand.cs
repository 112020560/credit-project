using Crm.Application.Abstractions.Messaging;
using Crm.Application.RiskEngine.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.RiskEngine;

public record CreateRiskRuleCommand(CreateRiskRuleDto Dto) : ICommand<RiskRuleDto>;

internal sealed class CreateRiskRuleCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRiskRuleCommand, RiskRuleDto>
{
    public async Task<Result<RiskRuleDto>> Handle(CreateRiskRuleCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.Weight <= 0)
            return Result.Failure<RiskRuleDto>(RiskRuleError.InvalidWeight);

        if (!Domain.RiskEngine.RuleType.All.Contains(request.Dto.RuleType))
            return Result.Failure<RiskRuleDto>(RiskRuleError.UnknownType);

        var rule = new RiskRule
        {
            Id = Guid.CreateVersion7(),
            Name = request.Dto.Name,
            RuleType = request.Dto.RuleType,
            TargetField = request.Dto.TargetField,
            Parameters = request.Dto.Parameters,
            Weight = request.Dto.Weight,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.RiskRulesRepository.AddAsync(rule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RiskRuleDto(rule.Id, rule.Name, rule.RuleType, rule.TargetField, rule.Parameters, rule.Weight, rule.CreatedAt);
    }
}

internal sealed class CreateRiskRuleCommandValidator : AbstractValidator<CreateRiskRuleCommand>
{
    public CreateRiskRuleCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty();
        RuleFor(x => x.Dto.RuleType).NotEmpty();
        RuleFor(x => x.Dto.TargetField).NotEmpty();
        RuleFor(x => x.Dto.Weight).GreaterThan(0);
    }
}
