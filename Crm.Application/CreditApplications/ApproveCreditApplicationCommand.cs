using Crm.Application.Abstractions.Messaging;
using Crm.Application.ApprovalWorkflows;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Crm.Domain.CreditApplications;
using Crm.Domain.Customers;
using Crm.Domain.Prospects;
using FluentValidation;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;

namespace Crm.Application.CreditApplications;

public record ApproveCreditApplicationCommand(Guid ApplicationId) : ICommand;

internal sealed class ApproveCreditApplicationCommandHandler(
    IUnitOfWork unitOfWork,
    ApprovalWorkflowService approvalWorkflowService)
    : ICommandHandler<ApproveCreditApplicationCommand>
{
    public async Task<Result> Handle(ApproveCreditApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await unitOfWork.CreditApplicationsRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null)
            return Result.Failure(CreditApplicationError.NotFound(request.ApplicationId));

        return await approvalWorkflowService.RecordDecisionAsync(application, ApprovalDecisionOutcome.Approved, null, null, cancellationToken);
    }

    internal static Customer MapProspectToCustomer(Prospect prospect)
    {
        var now = DateTime.UtcNow;
        return new Customer
        {
            Id = Guid.CreateVersion7(),
            IdentificationType = prospect.IdentificationType,
            IdentificationNumber = prospect.IdentificationNumber,
            FullName = prospect.FullName,
            DisplayName = prospect.DisplayName,
            BirthDate = prospect.BirthDate,
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now,
            CustomerEmails = [.. prospect.Emails.Select(e => new CustomerEmail { Id = Guid.CreateVersion7(), Email = e.Email, IsPrimary = e.IsPrimary, Verified = e.Verified, CreatedAt = now })],
            CustomerPhones = [.. prospect.Phones.Select(p => new CustomerPhone { Id = Guid.CreateVersion7(), Type = p.Type, Number = p.Number, CountryCode = p.CountryCode, IsPrimary = p.IsPrimary, Verified = p.Verified, CreatedAt = now })],
            CustomerAddresses = [.. prospect.Addresses.Select(a => new CustomerAddress { Id = Guid.CreateVersion7(), Type = a.Type, Street = a.Street, City = a.City, State = a.State, Country = a.Country, PostalCode = a.PostalCode, IsPrimary = a.IsPrimary, CreatedAt = now, UpdatedAt = now })],
            CustomerWorkInfos = [.. prospect.WorkInfos.Select(w => new CustomerWorkInfo { Id = Guid.CreateVersion7(), Occupation = w.Occupation, EmployerName = w.EmployerName, Salary = w.Salary, CreatedAt = now, UpdatedAt = now })],
            CustomerFiscalInfos = [.. prospect.FiscalInfos.Select(f => new CustomerFiscalInfo { Id = Guid.CreateVersion7(), TaxId = f.TaxId, TaxRegime = f.TaxRegime, EconomicActivity = f.EconomicActivity, Industry = f.Industry, CreatedAt = now, UpdatedAt = now })],
        };
    }
}

internal sealed class ApproveCreditApplicationCommandValidator : AbstractValidator<ApproveCreditApplicationCommand>
{
    public ApproveCreditApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
