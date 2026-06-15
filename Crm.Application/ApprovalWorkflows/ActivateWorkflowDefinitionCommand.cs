using Crm.Application.Abstractions.Messaging;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.ApprovalWorkflows;

public record ActivateWorkflowDefinitionCommand(Guid WorkflowId) : ICommand;

internal sealed class ActivateWorkflowDefinitionCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<ActivateWorkflowDefinitionCommand>
{
    public async Task<Result> Handle(ActivateWorkflowDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await unitOfWork.WorkflowDefinitionsRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        if (definition is null)
            return Result.Failure(WorkflowError.NotFound(request.WorkflowId));

        if (definition.Status == WorkflowStatus.Active)
            return Result.Failure(WorkflowError.AlreadyActive);

        // Deactivate all currently active definitions
        var activeDefinitions = await unitOfWork.WorkflowDefinitionsRepository.GetAllActiveAsync(cancellationToken);
        foreach (var active in activeDefinitions)
        {
            active.Status = WorkflowStatus.Superseded;
            await unitOfWork.WorkflowDefinitionsRepository.UpdateAsync(active, cancellationToken);
        }

        definition.Status = WorkflowStatus.Active;
        await unitOfWork.WorkflowDefinitionsRepository.UpdateAsync(definition, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal sealed class ActivateWorkflowDefinitionCommandValidator : AbstractValidator<ActivateWorkflowDefinitionCommand>
{
    public ActivateWorkflowDefinitionCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
    }
}
