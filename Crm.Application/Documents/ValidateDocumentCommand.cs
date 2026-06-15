using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.Documents.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Documents;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.Documents;

public record ValidateDocumentCommand(Guid DocumentId, ValidateDocumentDto Dto) : ICommand;

internal sealed class ValidateDocumentCommandHandler(IUnitOfWork unitOfWork, IMqProducerService mqProducerService)
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
            Id = Guid.CreateVersion7(),
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

        var traceId = Guid.NewGuid().ToString("N");
        if (document.Status == DocumentStatus.Validated)
        {
            await mqProducerService.PublishEvent(
                new DocumentValidatedContract(document.Id, document.OwnerId, document.OwnerType, validation.ReviewedBy, validation.ReviewedAt),
                traceId, cancellationToken);
        }
        else
        {
            await mqProducerService.PublishEvent(
                new DocumentRejectedContract(document.Id, document.OwnerId, document.OwnerType, validation.RejectionReason, validation.ReviewedBy, validation.ReviewedAt),
                traceId, cancellationToken);
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
