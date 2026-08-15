using Crm.Application.Abstractions.Messaging;
using Crm.Application.ApprovalWorkflows;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Crm.Domain.CreditApplications;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.CreditApplications;

public record ApproveCreditApplicationCommand(Guid ApplicationId) : ICommand;

internal sealed class ApproveCreditApplicationCommandHandler(
    IUnitOfWork unitOfWork,
    ApprovalWorkflowService approvalWorkflowService)
    : ICommandHandler<ApproveCreditApplicationCommand>
{
    public async Task<Result> Handle(ApproveCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure(CreditApplicationError.NotFound(request.ApplicationId));

        return await approvalWorkflowService.RecordDecisionAsync(application, ApprovalDecisionOutcome.Approved, null, null, cancellationToken);
    }
}

internal sealed class ApproveCreditApplicationCommandValidator : AbstractValidator<ApproveCreditApplicationCommand>
{
    public ApproveCreditApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
