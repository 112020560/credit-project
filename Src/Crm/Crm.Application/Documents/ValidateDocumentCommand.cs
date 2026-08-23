using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.Documents.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Documents;
using FluentValidation;
using SharedKernel;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.Documents;

public record ValidateDocumentCommand(Guid DocumentId, ValidateDocumentDto Dto) : ICommand;

internal sealed class ValidateDocumentCommandHandler(IUnitOfWork unitOfWork, IOutboxWriter outbox)
    : ICommandHandler<ValidateDocumentCommand>
{
    public async Task<Result> Handle(ValidateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await unitOfWork.DocumentsRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure(DocumentError.NotFound(request.DocumentId));

        if (document.Status != DocumentStatus.Uploaded)
            return Result.Failure(DocumentError.InvalidTransition(document.Status));

        if (request.Dto.Decision == DocumentValidationDecision.Rejected && string.IsNullOrWhiteSpace(request.Dto.RejectionReason))
            return Result.Failure(DocumentError.RejectionReasonRequired);

        var validation = new DocumentValidation
        {
            DocumentId = document.Id,
            Decision = request.Dto.Decision,
            RejectionReason = request.Dto.RejectionReason,
            ReviewedAt = DateTime.UtcNow
        };

        document.Validations.Add(validation);
        document.Status = request.Dto.Decision == DocumentValidationDecision.Validated
            ? DocumentStatus.Validated
            : DocumentStatus.Rejected;
        document.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.DocumentsRepository.UpdateAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (document.Status == DocumentStatus.Validated)
        {
            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = document.Id,
                AggregateType    = "Document",
                EventType        = "DocumentValidated",
                DeduplicationKey = $"DocumentValidated:{document.Id}",
                Payload          = JsonSerializer.Serialize(new DocumentValidatedContract(document.Id, document.OwnerId, document.OwnerType, validation.ReviewedBy, validation.ReviewedAt))
            }, cancellationToken);
        }
        else
        {
            await outbox.AppendAsync(new OutboxEvent
            {
                ServiceName      = "crm",
                AggregateId      = document.Id,
                AggregateType    = "Document",
                EventType        = "DocumentRejected",
                DeduplicationKey = $"DocumentRejected:{document.Id}",
                Payload          = JsonSerializer.Serialize(new DocumentRejectedContract(document.Id, document.OwnerId, document.OwnerType, validation.RejectionReason, validation.ReviewedBy, validation.ReviewedAt))
            }, cancellationToken);
        }

        return Result.Success();
    }
}

internal sealed class ValidateDocumentCommandValidator : AbstractValidator<ValidateDocumentCommand>
{
    public ValidateDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.Dto.Decision).NotEmpty();
        RuleFor(x => x.Dto.RejectionReason)
            .NotEmpty()
            .When(x => x.Dto.Decision == DocumentValidationDecision.Rejected);
    }
}
