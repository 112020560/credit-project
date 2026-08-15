using System.Text.Json;
using Crm.Application.ApprovalWorkflows.Dtos;
using Crm.Application.Customers.Dtos;
using Crm.Application.Prospects.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Crm.Domain.CreditApplications;
using Crm.Domain.Customers;
using Crm.Domain.Prospects;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.ApprovalWorkflows;

public class ApprovalWorkflowService(IUnitOfWork unitOfWork, IOutboxWriter outbox)
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

        if (decision == ApprovalDecisionOutcome.Rejected)
        {
            var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(application.ProspectId, cancellationToken);
            if (prospect is not null)
            {
                prospect.Reject();
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);
            }

            application.Reject(rejectionReason);
            await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = application.Id,
                AggregateType    = "CreditApplication",
                EventType        = "CreditApplicationRejected",
                DeduplicationKey = $"CreditApplicationRejected:{application.Id}",
                Payload          = JsonSerializer.Serialize(new ApplicationRejectedContract(application.Id, application.ProspectId, workflow?.Id, rejectionReason, approvalDecision.DecidedAt))
            }, cancellationToken);

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

            var customer = Customer.FromProspect(prospect);
            await unitOfWork.CustomersRepository.AddCustomerAsync(customer, cancellationToken);

            prospect.Convert();
            await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);

            application.Approve();
            await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = application.Id,
                AggregateType    = "CreditApplication",
                EventType        = "CreditApplicationApproved",
                DeduplicationKey = $"CreditApplicationApproved:{application.Id}",
                Payload          = JsonSerializer.Serialize(new ApplicationApprovedContract(application.Id, application.ProspectId, workflow?.Id, approvalDecision.DecidedAt))
            }, cancellationToken);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = prospect.Id,
                AggregateType    = "Prospect",
                EventType        = "ProspectConverted",
                DeduplicationKey = $"ProspectConverted:{prospect.Id}",
                Payload          = JsonSerializer.Serialize(new ProspectConvertedContract { ProspectId = prospect.Id, CustomerId = customer.Id })
            }, cancellationToken);

            var evaluations = await unitOfWork.RiskEvaluationsRepository.GetByCreditApplicationIdAsync(application.Id, cancellationToken);
            var latestEval = evaluations.OrderByDescending(e => e.EvaluatedAt).FirstOrDefault();

            var contract = new CreateCustomerContract
            {
                CustomerId           = customer.Id,
                FullName             = customer.FullName,
                DisplayName          = customer.DisplayName ?? string.Empty,
                IdentificationType   = customer.IdentificationType,
                IdentificationNumber = customer.IdentificationNumber ?? string.Empty,
                TaxId                = prospect.FiscalInfos.FirstOrDefault()?.TaxId,
                Email                = customer.CustomerEmails.FirstOrDefault(e => e.IsPrimary == true)?.Email ?? customer.CustomerEmails.FirstOrDefault()?.Email,
                Phone                = customer.CustomerPhones.FirstOrDefault(p => p.IsPrimary == true)?.Number ?? customer.CustomerPhones.FirstOrDefault()?.Number,
                CreatedAt            = DateTimeOffset.UtcNow,
                Version              = 1,
                Metadata             = latestEval is not null ? new Dictionary<string, object> { ["CreditScore"] = latestEval.TotalScore } : null
            };
            var customerPayload = JsonSerializer.Serialize(contract);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = customer.Id,
                AggregateType    = "Customer",
                EventType        = "CustomerCreated",
                DeduplicationKey = $"CustomerCreated:{customer.Id}",
                Payload          = customerPayload
            }, cancellationToken);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = customer.Id,
                AggregateType    = "Customer",
                EventType        = "CustomerCreatedCommand",
                DeduplicationKey = $"CustomerCreatedCommand:{customer.Id}",
                Payload          = customerPayload
            }, cancellationToken);
        }
        else
        {
            // More steps remain — stay in InReview, publish ApprovalRequested for next step
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var nextStep = workflow!.Steps.OrderBy(s => s.Order).First(s => s.Order > pendingStep!.Order);
            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = application.Id,
                AggregateType    = "CreditApplication",
                EventType        = "ApprovalRequested",
                DeduplicationKey = $"ApprovalRequested:{application.Id}:{nextStep.Id}",
                Payload          = JsonSerializer.Serialize(new ApprovalRequestedContract(application.Id, workflow.Id, nextStep.Id, nextStep.StepName, nextStep.Order, DateTime.UtcNow))
            }, cancellationToken);
        }

        return Result.Success();
    }

    internal static WorkflowDefinitionDto ToDto(WorkflowDefinition w) => new(
        w.Id, w.Name, w.Status,
        w.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto(s.Id, s.StepName, s.Order, s.RequiredRole)).ToList(),
        w.CreatedAt);
}
