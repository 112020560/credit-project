using Crm.Application.Abstractions.Messaging;
using Crm.Application.ApprovalWorkflows;
using Crm.Application.CreditApplications.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Crm.Domain.CreditApplications;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.CreditApplications;

public record RejectCreditApplicationCommand(Guid ApplicationId, RejectCreditApplicationDto Dto) : ICommand;

internal sealed class RejectCreditApplicationCommandHandler(
    IUnitOfWork unitOfWork,
    ApprovalWorkflowService approvalWorkflowService)
    : ICommandHandler<RejectCreditApplicationCommand>
{
    public async Task<Result> Handle(RejectCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure(CreditApplicationError.NotFound(request.ApplicationId));

        return await approvalWorkflowService.RecordDecisionAsync(application, ApprovalDecisionOutcome.Rejected, request.Dto.Reason, null, cancellationToken);
    }
}

internal sealed class RejectCreditApplicationCommandValidator : AbstractValidator<RejectCreditApplicationCommand>
{
    public RejectCreditApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Dto.Reason).NotEmpty().WithMessage("Rejection reason is required");
    }
}
