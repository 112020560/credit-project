using Crm.Application.Abstractions.Messaging;
using Crm.Application.CreditApplications.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.CreditApplications;

public record RegisterApplicationDocumentCommand(Guid ApplicationId, RegisterApplicationDocumentDto Dto) : ICommand<ApplicationDocumentDto>;

internal sealed class RegisterApplicationDocumentCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterApplicationDocumentCommand, ApplicationDocumentDto>
{
    public async Task<Result<ApplicationDocumentDto>> Handle(RegisterApplicationDocumentCommand request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure<ApplicationDocumentDto>(CreditApplicationError.NotFound(request.ApplicationId));

        if (application.Status is not (CreditApplicationStatus.Draft or CreditApplicationStatus.Submitted))
            return Result.Failure<ApplicationDocumentDto>(CreditApplicationError.InvalidTransition(application.Status, "document registration"));

        var doc = new ApplicationDocument
        {
            Id = Guid.CreateVersion7(),
            CreditApplicationId = application.Id,
            Type = request.Dto.Type,
            StorageUrl = request.Dto.StorageUrl,
            Status = ApplicationDocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow,
        };

        application.Documents.Add(doc);
        await unitOfWork.CreditApplicationsRepository.UpdateAsync(application, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApplicationDocumentDto(doc.Id, doc.Type, doc.StorageUrl, doc.Status, doc.UploadedAt);
    }
}

internal sealed class RegisterApplicationDocumentCommandValidator : AbstractValidator<RegisterApplicationDocumentCommand>
{
    public RegisterApplicationDocumentCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Dto.Type)
            .NotEmpty()
            .Must(ApplicationDocumentType.All.Contains)
            .WithMessage($"Document type must be one of: {string.Join(", ", ApplicationDocumentType.All)}");
        RuleFor(x => x.Dto.StorageUrl).NotEmpty();
    }
}
