using Crm.Application.Abstractions.Messaging;
using Crm.Application.Prospects.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Prospects;
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
                prospect.Phones.Add(new ProspectPhone { Id = Guid.CreateVersion7(), Type = contact.Type, Number = contact.Value, IsPrimary = contact.IsPrimary, Verified = false, CreatedAt = now });
            else if (contact.Type == "Email")
                prospect.Emails.Add(new ProspectEmail { Id = Guid.CreateVersion7(), Email = contact.Value, IsPrimary = contact.IsPrimary, Verified = false, CreatedAt = now });
        }

        foreach (var address in dto.Addresses ?? [])
            prospect.Addresses.Add(new ProspectAddress { Id = Guid.CreateVersion7(), Type = address.Type, Street = address.Street, City = address.City, State = address.State, Country = address.Country, PostalCode = address.PostalCode, IsPrimary = address.IsPrimary, CreatedAt = now, UpdatedAt = now });

        foreach (var work in dto.WorkInfos ?? [])
            prospect.WorkInfos.Add(new ProspectWorkInfo { Id = Guid.CreateVersion7(), Occupation = work.Occupation, EmployerName = work.EmployerName, Salary = work.Salary, CreatedAt = now, UpdatedAt = now });

        foreach (var fiscal in dto.FiscalInfos ?? [])
            prospect.FiscalInfos.Add(new ProspectFiscalInfo { Id = Guid.CreateVersion7(), TaxId = fiscal.TaxId, TaxRegime = fiscal.TaxRegime, EconomicActivity = fiscal.EconomicActivity, Industry = fiscal.Industry, CreatedAt = now, UpdatedAt = now });

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
