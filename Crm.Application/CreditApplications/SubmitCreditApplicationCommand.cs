using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.ApprovalWorkflows.Dtos;
using Crm.Application.CreditApplications.Dtos;
using Crm.Application.Customers.Dtos;
using Crm.Application.Prospects.Dtos;
using Crm.Application.RiskEngine;
using Crm.Application.RiskEngine.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using Crm.Domain.Customers;
using Crm.Domain.Prospects;
using Crm.Domain.RiskEngine;
using FluentValidation;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.CreditApplications;

public record SubmitCreditApplicationCommand(Guid ApplicationId) : ICommand;

internal sealed class SubmitCreditApplicationCommandHandler(
    IUnitOfWork unitOfWork,
    RiskEvaluationService riskEvaluationService,
    IOutboxWriter outbox)
    : ICommandHandler<SubmitCreditApplicationCommand>
{
    public async Task<Result> Handle(SubmitCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure(CreditApplicationError.NotFound(request.ApplicationId));

        if (application.Status != CreditApplicationStatus.Draft)
            return Result.Failure(CreditApplicationError.InvalidTransition(application.Status, CreditApplicationStatus.Submitted));

        var uploadedTypes = application.Documents
            .Where(d => d.Status is ApplicationDocumentStatus.Uploaded or ApplicationDocumentStatus.Verified)
            .Select(d => d.Type)
            .ToHashSet();

        var missing = ApplicationDocumentType.Required.Except(uploadedTypes).ToList();
        if (missing.Count > 0)
            return Result.Failure(CreditApplicationError.MissingDocuments(missing));

        var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(application.ProspectId, cancellationToken);
        if (prospect is null)
            return Result.Failure(ProspectError.NotFound(application.ProspectId));

        var (evaluation, hasMatrix) = await riskEvaluationService.EvaluateAsync(application, prospect, cancellationToken);
        if (!hasMatrix)
            return Result.Failure(RiskMatrixError.NoActiveMatrix);

        Customer? createdCustomer = null;

        switch (evaluation.Outcome)
        {
            case RiskEvaluationOutcome.AutoApprove:
                createdCustomer = Customer.FromProspect(prospect);
                await unitOfWork.CustomersRepository.AddCustomerAsync(createdCustomer, cancellationToken);
                prospect.Convert();
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);
                application.Approve();
                await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
                break;

            case RiskEvaluationOutcome.AutoReject:
                prospect.Reject();
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);
                application.Reject($"Auto-rejected by risk engine (score: {evaluation.TotalScore})");
                await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
                break;

            default: // ManualReview
                var activeWorkflow = await unitOfWork.WorkflowDefinitionsRepository.GetActiveAsync(cancellationToken);
                prospect.Submit();
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);
                application.SendToReview(activeWorkflow?.Id);
                await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await outbox.AppendAsync(new OutboxEvent
        {
            ServiceName      = "crm",
            AggregateId      = application.Id,
            AggregateType    = "CreditApplication",
            EventType        = "CreditApplicationSubmitted",
            DeduplicationKey = $"CreditApplicationSubmitted:{application.Id}",
            Payload          = JsonSerializer.Serialize(new CreditApplicationSubmittedContract { ApplicationId = application.Id, ProspectId = application.ProspectId })
        }, cancellationToken);

        if (evaluation.Outcome == RiskEvaluationOutcome.ManualReview && application.WorkflowDefinitionId.HasValue)
        {
            var wf = await unitOfWork.WorkflowDefinitionsRepository.GetByIdAsync(application.WorkflowDefinitionId.Value, cancellationToken);
            var firstStep = wf?.Steps.OrderBy(s => s.Order).FirstOrDefault();
            if (firstStep is not null)
                await outbox.AppendAsync(new OutboxEvent
                {
                    ServiceName      = "crm",
                    AggregateId      = application.Id,
                    AggregateType    = "CreditApplication",
                    EventType        = "ApprovalRequested",
                    DeduplicationKey = $"ApprovalRequested:{application.Id}:{firstStep.Id}",
                    Payload          = JsonSerializer.Serialize(new ApprovalRequestedContract(application.Id, wf!.Id, firstStep.Id, firstStep.StepName, firstStep.Order, DateTime.UtcNow))
                }, cancellationToken);
        }

        await outbox.AppendAsync(new OutboxEvent
        {
            ServiceName      = "crm",
            AggregateId      = evaluation.CreditApplicationId,
            AggregateType    = "CreditApplication",
            EventType        = "RiskEvaluationCompleted",
            DeduplicationKey = $"RiskEvaluationCompleted:{application.Id}",
            Payload          = JsonSerializer.Serialize(new RiskEvaluationCompletedContract
            {
                RiskEvaluationId    = evaluation.Id,
                CreditApplicationId = evaluation.CreditApplicationId,
                RiskMatrixId        = evaluation.RiskMatrixId,
                RiskMatrixVersion   = evaluation.RiskMatrixVersion,
                TotalScore          = evaluation.TotalScore,
                Outcome             = evaluation.Outcome.ToString(),
                CompletedAt         = evaluation.EvaluatedAt
            })
        }, cancellationToken);

        if (evaluation.Outcome == RiskEvaluationOutcome.AutoApprove && createdCustomer is not null)
        {
            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = application.Id,
                AggregateType    = "CreditApplication",
                EventType        = "CreditApplicationApproved",
                DeduplicationKey = $"CreditApplicationApproved:{application.Id}",
                Payload          = JsonSerializer.Serialize(new CreditApplicationApprovedContract { ApplicationId = application.Id, ProspectId = prospect.Id, Status = CreditApplicationStatus.Approved.ToString() })
            }, cancellationToken);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = prospect.Id,
                AggregateType    = "Prospect",
                EventType        = "ProspectConverted",
                DeduplicationKey = $"ProspectConverted:{prospect.Id}",
                Payload          = JsonSerializer.Serialize(new ProspectConvertedContract { ProspectId = prospect.Id, CustomerId = createdCustomer.Id })
            }, cancellationToken);

            var contract = new CreateCustomerContract
            {
                CustomerId           = createdCustomer.Id,
                FullName             = createdCustomer.FullName,
                DisplayName          = createdCustomer.DisplayName ?? string.Empty,
                IdentificationType   = createdCustomer.IdentificationType,
                IdentificationNumber = createdCustomer.IdentificationNumber ?? string.Empty,
                TaxId                = prospect.FiscalInfos.FirstOrDefault()?.TaxId,
                Email                = createdCustomer.CustomerEmails.FirstOrDefault(e => e.IsPrimary == true)?.Email ?? createdCustomer.CustomerEmails.FirstOrDefault()?.Email,
                Phone                = createdCustomer.CustomerPhones.FirstOrDefault(p => p.IsPrimary == true)?.Number ?? createdCustomer.CustomerPhones.FirstOrDefault()?.Number,
                CreatedAt            = DateTimeOffset.UtcNow,
                Version              = 1,
                Metadata             = new Dictionary<string, object> { ["CreditScore"] = evaluation.TotalScore }
            };
            var customerPayload = JsonSerializer.Serialize(contract);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = createdCustomer.Id,
                AggregateType    = "Customer",
                EventType        = "CustomerCreated",
                DeduplicationKey = $"CustomerCreated:{createdCustomer.Id}",
                Payload          = customerPayload
            }, cancellationToken);

            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = createdCustomer.Id,
                AggregateType    = "Customer",
                EventType        = "CustomerCreatedCommand",
                DeduplicationKey = $"CustomerCreatedCommand:{createdCustomer.Id}",
                Payload          = customerPayload
            }, cancellationToken);
        }
        else if (evaluation.Outcome == RiskEvaluationOutcome.AutoReject)
        {
            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = application.Id,
                AggregateType    = "CreditApplication",
                EventType        = "CreditApplicationRejected",
                DeduplicationKey = $"CreditApplicationRejected:{application.Id}",
                Payload          = JsonSerializer.Serialize(new CreditApplicationRejectedContract { ApplicationId = application.Id, ProspectId = application.ProspectId, RejectionReason = application.RejectionReason })
            }, cancellationToken);
        }

        return Result.Success();
    }
}

internal sealed class SubmitCreditApplicationCommandValidator : AbstractValidator<SubmitCreditApplicationCommand>
{
    public SubmitCreditApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
