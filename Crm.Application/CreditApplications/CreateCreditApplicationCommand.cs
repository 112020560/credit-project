using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.CreditApplications.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using Crm.Domain.Prospects;
using FluentValidation;
using SharedKernel;
using SharedKernel.Contracts.Crm.CreditApplications;

namespace Crm.Application.CreditApplications;

public record CreateCreditApplicationCommand(CreateCreditApplicationDto Dto) : ICommand<CreditApplicationDetailDto>;

internal sealed class CreateCreditApplicationCommandHandler(
    IUnitOfWork unitOfWork,
    IMqProducerService mqProducerService)
    : ICommandHandler<CreateCreditApplicationCommand, CreditApplicationDetailDto>
{
    public async Task<Result<CreditApplicationDetailDto>> Handle(CreateCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(request.Dto.ProspectId, cancellationToken);
        if (prospect is null)
            return Result.Failure<CreditApplicationDetailDto>(ProspectError.NotFound(request.Dto.ProspectId));

        if (prospect.Status == ProspectStatus.Converted)
            return Result.Failure<CreditApplicationDetailDto>(ProspectError.AlreadyConverted);

        var application = new CreditApplication
        {
            Id = Guid.CreateVersion7(),
            ProspectId = prospect.Id,
            Status = CreditApplicationStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await unitOfWork.CreditApplicationsRepository.AddAsync(application, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await mqProducerService.PublishEvent(new CreditApplicationCreatedContract { ApplicationId = application.Id, ProspectId = application.ProspectId, Status = application.Status.ToString() }, Guid.NewGuid().ToString("N"), cancellationToken);

        return ToDto(application);
    }

    internal static CreditApplicationDetailDto ToDto(CreditApplication a) =>
        new(a.Id, a.ProspectId, a.Status, a.RejectionReason, a.CreatedAt, a.UpdatedAt,
            a.Documents.Select(d => new ApplicationDocumentDto(d.Id, d.Type, d.StorageUrl, d.Status, d.UploadedAt)).ToList());
}

internal sealed class CreateCreditApplicationCommandValidator : AbstractValidator<CreateCreditApplicationCommand>
{
    public CreateCreditApplicationCommandValidator()
    {
        RuleFor(x => x.Dto.ProspectId).NotEmpty();
    }
}
