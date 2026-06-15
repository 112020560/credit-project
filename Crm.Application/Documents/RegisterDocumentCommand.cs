using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.Documents.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Documents;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.Documents;

public record RegisterDocumentCommand(RegisterDocumentDto Dto) : ICommand<DocumentDetailDto>;

internal sealed class RegisterDocumentCommandHandler(IUnitOfWork unitOfWork, IMqProducerService mqProducerService)
    : ICommandHandler<RegisterDocumentCommand, DocumentDetailDto>
{
    public async Task<Result<DocumentDetailDto>> Handle(RegisterDocumentCommand request, CancellationToken cancellationToken)
    {
        var docType = await unitOfWork.DocumentTypesRepository.GetByCodeAsync(request.Dto.DocumentTypeCode, cancellationToken);
        if (docType is null)
            return Result.Failure<DocumentDetailDto>(DocumentError.InvalidDocumentType(request.Dto.DocumentTypeCode));

        var document = new Document
        {
            Id = Guid.CreateVersion7(),
            OwnerId = request.Dto.OwnerId,
            OwnerType = request.Dto.OwnerType,
            DocumentTypeCode = request.Dto.DocumentTypeCode,
            StorageUrl = request.Dto.StorageUrl,
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await unitOfWork.DocumentsRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");
        await mqProducerService.PublishEvent(
            new DocumentUploadedContract(document.Id, document.OwnerId, document.OwnerType, document.DocumentTypeCode, document.StorageUrl, document.UploadedAt),
            traceId, cancellationToken);

        return ToDto(document);
    }

    internal static DocumentDetailDto ToDto(Document d) => new(
        d.Id, d.OwnerId, d.OwnerType, d.DocumentTypeCode, d.StorageUrl, d.Status, d.UploadedAt, d.UpdatedAt,
        d.Validations.Select(v => new DocumentValidationDto(v.Id, v.Decision, v.RejectionReason, v.ReviewedBy, v.ReviewedAt)).ToList());
}

internal sealed class RegisterDocumentCommandValidator : AbstractValidator<RegisterDocumentCommand>
{
    public RegisterDocumentCommandValidator()
    {
        RuleFor(x => x.Dto.OwnerId).NotEmpty();
        RuleFor(x => x.Dto.OwnerType).NotEmpty();
        RuleFor(x => x.Dto.DocumentTypeCode).NotEmpty();
        RuleFor(x => x.Dto.StorageUrl).NotEmpty();
    }
}
