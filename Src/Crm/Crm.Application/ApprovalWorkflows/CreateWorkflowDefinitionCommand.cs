using Crm.Application.Abstractions.Messaging;
using Crm.Application.ApprovalWorkflows.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.ApprovalWorkflows;

public record CreateWorkflowDefinitionCommand(CreateWorkflowDefinitionDto Dto) : ICommand<WorkflowDefinitionDto>;

internal sealed class CreateWorkflowDefinitionCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<CreateWorkflowDefinitionCommand, WorkflowDefinitionDto>
{
    public async Task<Result<WorkflowDefinitionDto>> Handle(CreateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.Steps.Count == 0)
            return Result.Failure<WorkflowDefinitionDto>(WorkflowError.NoSteps);

        var now = DateTime.UtcNow;
        var definition = new WorkflowDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = request.Dto.Name,
            Status = WorkflowStatus.Draft,
            CreatedAt = now,
            Steps = request.Dto.Steps.Select(s => new WorkflowStep
            {
                Id = Guid.CreateVersion7(),
                StepName = s.StepName,
                Order = s.Order,
                RequiredRole = s.RequiredRole,
                CreatedAt = now
            }).ToList()
        };

        await unitOfWork.WorkflowDefinitionsRepository.AddAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApprovalWorkflowService.ToDto(definition);
    }
}

internal sealed class CreateWorkflowDefinitionCommandValidator : AbstractValidator<CreateWorkflowDefinitionCommand>
{
    public CreateWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty();
        RuleFor(x => x.Dto.Steps).NotEmpty().WithMessage("At least one workflow step is required");
    }
}
