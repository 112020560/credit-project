using Crm.Application.Abstractions.Messaging;
using Crm.Application.Prospects.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Prospects;
using Crm.Domain.ValueObjects;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.Prospects;

public record EnrichProspectCommand(Guid ProspectId, EnrichProspectDto Dto) : ICommand;

internal sealed class EnrichProspectCommandHandler(IUnitOfWork unitOfWork)
    : ICommandHandler<EnrichProspectCommand>
{
    public async Task<Result> Handle(EnrichProspectCommand request, CancellationToken cancellationToken)
    {
        var prospect = await unitOfWork.ProspectsRepository.GetByIdAsync(request.ProspectId, cancellationToken);
        if (prospect is null)
            return Result.Failure(ProspectError.NotFound(request.ProspectId));

        if (prospect.Status == ProspectStatus.Converted)
            return Result.Failure(ProspectError.AlreadyConverted);

        var dto = request.Dto;
        var now = DateTime.UtcNow;

        foreach (var contact in dto.Contacts ?? [])
        {
            if (contact.Type == "Phone")
                prospect.Phones.Add(new PhoneContact(contact.Value, contact.Type, null, contact.IsPrimary, false));
            else if (contact.Type == "Email")
                prospect.Emails.Add(new EmailContact(contact.Value, contact.IsPrimary, false));
        }

        foreach (var address in dto.Addresses ?? [])
            prospect.Addresses.Add(new Address(address.Type, address.Street, address.City, address.State, address.Country, address.PostalCode, address.IsPrimary));

        foreach (var work in dto.WorkInfos ?? [])
            prospect.WorkInfos.Add(new WorkInfo(work.Occupation, work.EmployerName, work.Salary));

        foreach (var fiscal in dto.FiscalInfos ?? [])
            prospect.FiscalInfos.Add(new FiscalInfo(fiscal.TaxId, fiscal.TaxRegime, fiscal.EconomicActivity, fiscal.Industry));

        prospect.UpdatedAt = now;
        await unitOfWork.ProspectsRepository.UpdateAsync(prospect, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal sealed class EnrichProspectCommandValidator : AbstractValidator<EnrichProspectCommand>
{
    public EnrichProspectCommandValidator()
    {
        RuleFor(x => x.ProspectId).NotEmpty();
    }
}
