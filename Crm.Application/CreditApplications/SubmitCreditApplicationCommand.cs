using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.ApprovalWorkflows.Dtos;
using Crm.Application.Customers.Dtos;
using Crm.Application.RiskEngine;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using Crm.Domain.Customers;
using Crm.Domain.Prospects;
using Crm.Domain.RiskEngine;
using FluentValidation;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;

namespace Crm.Application.CreditApplications;

public record SubmitCreditApplicationCommand(Guid ApplicationId) : ICommand;

internal sealed class SubmitCreditApplicationCommandHandler(
    IUnitOfWork unitOfWork,
    RiskEvaluationService riskEvaluationService,
    IMqProducerService mqProducerService)
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
                createdCustomer = ApproveCreditApplicationCommandHandler.MapProspectToCustomer(prospect);
                await unitOfWork.CustomersRepository.AddCustomerAsync(createdCustomer, cancellationToken);

                prospect.Status = ProspectStatus.Converted;
                prospect.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);

                application.Status = CreditApplicationStatus.Approved;
                application.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
                break;

            case RiskEvaluationOutcome.AutoReject:
                prospect.Status = ProspectStatus.Draft;
                prospect.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);

                application.Status = CreditApplicationStatus.Rejected;
                application.RejectionReason = $"Auto-rejected by risk engine (score: {evaluation.TotalScore})";
                application.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
                break;

            default: // ManualReview
                var activeWorkflow = await unitOfWork.WorkflowDefinitionsRepository.GetActiveAsync(cancellationToken);

                prospect.Status = ProspectStatus.Submitted;
                prospect.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);

                application.Status = CreditApplicationStatus.InReview;
                application.WorkflowDefinitionId = activeWorkflow?.Id;
                application.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");

        await mqProducerService.PublishEvent(new { ApplicationId = application.Id, application.ProspectId }, traceId, cancellationToken);

        if (evaluation.Outcome == RiskEvaluationOutcome.ManualReview && application.WorkflowDefinitionId.HasValue)
        {
            var wf = await unitOfWork.WorkflowDefinitionsRepository.GetByIdAsync(application.WorkflowDefinitionId.Value, cancellationToken);
            var firstStep = wf?.Steps.OrderBy(s => s.Order).FirstOrDefault();
            if (firstStep is not null)
                await mqProducerService.PublishEvent(
                    new ApprovalRequestedContract(application.Id, wf!.Id, firstStep.Id, firstStep.StepName, firstStep.Order, DateTime.UtcNow),
                    traceId, cancellationToken);
        }
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

        if (evaluation.Outcome == RiskEvaluationOutcome.AutoApprove && createdCustomer is not null)
        {
            await mqProducerService.PublishEvent(new { ApplicationId = application.Id, prospect.Id, Status = CreditApplicationStatus.Approved }, traceId, cancellationToken);
            await mqProducerService.PublishEvent(new { ProspectId = prospect.Id, CustomerId = createdCustomer.Id }, traceId, cancellationToken);

            var contract = new CreateCustomerContract
            {
                CustomerId = createdCustomer.Id,
                FullName = createdCustomer.FullName,
                DisplayName = createdCustomer.DisplayName ?? string.Empty,
                IdentificationType = createdCustomer.IdentificationType,
                IdentificationNumber = createdCustomer.IdentificationNumber ?? string.Empty,
                TaxId = prospect.FiscalInfos.FirstOrDefault()?.TaxId,
                Email = createdCustomer.CustomerEmails.FirstOrDefault(e => e.IsPrimary == true)?.Email ?? createdCustomer.CustomerEmails.FirstOrDefault()?.Email,
                Phone = createdCustomer.CustomerPhones.FirstOrDefault(p => p.IsPrimary == true)?.Number ?? createdCustomer.CustomerPhones.FirstOrDefault()?.Number,
                CreatedAt = DateTimeOffset.UtcNow,
                Version = 1,
                Metadata = null
            };
            await mqProducerService.SendCommand<CustomerCreated>(contract, "credit-service-customer-events", traceId, cancellationToken);
            await mqProducerService.PublishEvent(contract, traceId, cancellationToken);
        }
        else if (evaluation.Outcome == RiskEvaluationOutcome.AutoReject)
        {
            await mqProducerService.PublishEvent(new { ApplicationId = application.Id, application.ProspectId, application.RejectionReason }, traceId, cancellationToken);
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
