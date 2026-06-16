using Crm.Application.Abstractions.Mq;
using Crm.Application.ApprovalWorkflows.Dtos;
using Crm.Application.CreditApplications;
using Crm.Application.Customers.Dtos;
using Crm.Application.Prospects.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Crm.Domain.CreditApplications;
using Crm.Domain.Prospects;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;

namespace Crm.Application.ApprovalWorkflows;

public class ApprovalWorkflowService(IUnitOfWork unitOfWork, IMqProducerService mqProducerService)
{
    public async Task<Result> RecordDecisionAsync(
        CreditApplication application,
        string decision,
        string? rejectionReason,
        string? decidedBy,
        CancellationToken cancellationToken)
    {
        if (application.Status != CreditApplicationStatus.InReview)
            return Result.Failure(ApprovalError.ApplicationNotInReview);

        if (decision == ApprovalDecisionOutcome.Rejected && string.IsNullOrWhiteSpace(rejectionReason))
            return Result.Failure(ApprovalError.RejectionReasonRequired);

        // Load workflow and existing decisions
        WorkflowDefinition? workflow = null;
        if (application.WorkflowDefinitionId.HasValue)
            workflow = await unitOfWork.WorkflowDefinitionsRepository.GetByIdAsync(application.WorkflowDefinitionId.Value, cancellationToken);

        var existingDecisions = await unitOfWork.ApprovalDecisionsRepository.GetByApplicationIdAsync(application.Id, cancellationToken);
        var decidedStepIds = existingDecisions.Select(d => d.WorkflowStepId).ToHashSet();

        // Identify the pending step (null when using single-agent fallback)
        var pendingStep = workflow?.Steps.OrderBy(s => s.Order).FirstOrDefault(s => !decidedStepIds.Contains(s.Id));

        // Record the decision
        var approvalDecision = new ApprovalDecision
        {
            Id = Guid.CreateVersion7(),
            CreditApplicationId = application.Id,
            WorkflowDefinitionId = workflow?.Id,
            WorkflowStepId = pendingStep?.Id,
            Decision = decision,
            RejectionReason = rejectionReason,
            DecidedBy = decidedBy,
            DecidedAt = DateTime.UtcNow
        };
        await unitOfWork.ApprovalDecisionsRepository.AddAsync(approvalDecision, cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");

        if (decision == ApprovalDecisionOutcome.Rejected)
        {
            var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(application.ProspectId, cancellationToken);
            if (prospect is not null)
            {
                prospect.Status = ProspectStatus.Draft;
                prospect.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);
            }

            application.Status = CreditApplicationStatus.Rejected;
            application.RejectionReason = rejectionReason;
            application.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await mqProducerService.PublishEvent(
                new ApplicationRejectedContract(application.Id, application.ProspectId, workflow?.Id, rejectionReason, approvalDecision.DecidedAt),
                traceId, cancellationToken);

            return Result.Success();
        }

        // Approved decision — check if more steps remain
        bool allStepsComplete = workflow is null
            || pendingStep is null
            || !workflow.Steps.Any(s => s.Order > pendingStep.Order);

        if (allStepsComplete)
        {
            var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(application.ProspectId, cancellationToken);
            if (prospect is null)
                return Result.Failure(ProspectError.NotFound(application.ProspectId));

            var customer = ApproveCreditApplicationCommandHandler.MapProspectToCustomer(prospect);
            await unitOfWork.CustomersRepository.AddCustomerAsync(customer, cancellationToken);

            prospect.Status = ProspectStatus.Converted;
            prospect.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);

            application.Status = CreditApplicationStatus.Approved;
            application.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await mqProducerService.PublishEvent(
                new ApplicationApprovedContract(application.Id, application.ProspectId, workflow?.Id, approvalDecision.DecidedAt),
                traceId, cancellationToken);
            await mqProducerService.PublishEvent(new ProspectConvertedContract { ProspectId = prospect.Id, CustomerId = customer.Id }, traceId, cancellationToken);

            var contract = new CreateCustomerContract
            {
                CustomerId = customer.Id,
                FullName = customer.FullName,
                DisplayName = customer.DisplayName ?? string.Empty,
                IdentificationType = customer.IdentificationType,
                IdentificationNumber = customer.IdentificationNumber ?? string.Empty,
                TaxId = prospect.FiscalInfos.FirstOrDefault()?.TaxId,
                Email = customer.CustomerEmails.FirstOrDefault(e => e.IsPrimary == true)?.Email ?? customer.CustomerEmails.FirstOrDefault()?.Email,
                Phone = customer.CustomerPhones.FirstOrDefault(p => p.IsPrimary == true)?.Number ?? customer.CustomerPhones.FirstOrDefault()?.Number,
                CreatedAt = DateTimeOffset.UtcNow,
                Version = 1,
                Metadata = null
            };
            await mqProducerService.SendCommand<CustomerCreated>(contract, "credit-service-customer-events", traceId, cancellationToken);
            await mqProducerService.PublishEvent(contract, traceId, cancellationToken);
        }
        else
        {
            // More steps remain — stay in InReview, publish ApprovalRequested for next step
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var nextStep = workflow!.Steps.OrderBy(s => s.Order).First(s => s.Order > pendingStep!.Order);
            await mqProducerService.PublishEvent(
                new ApprovalRequestedContract(application.Id, workflow.Id, nextStep.Id, nextStep.StepName, nextStep.Order, DateTime.UtcNow),
                traceId, cancellationToken);
        }

        return Result.Success();
    }

    internal static WorkflowDefinitionDto ToDto(WorkflowDefinition w) => new(
        w.Id, w.Name, w.Status,
        w.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto(s.Id, s.StepName, s.Order, s.RequiredRole)).ToList(),
        w.CreatedAt);
}
