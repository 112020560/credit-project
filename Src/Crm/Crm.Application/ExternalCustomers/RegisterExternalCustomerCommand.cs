using Crm.Application.Abstractions.Messaging;
using Crm.Application.ExternalCustomers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using FluentValidation;
using SharedKernel;

namespace Crm.Application.ExternalCustomers;

public record RegisterExternalCustomerCommand(RegisterExternalCustomerDto Dto) : ICommand;

internal sealed class RegisterExternalCustomerCommandHandler(
    IExternalCustomerRefRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterExternalCustomerCommand>
{
    public async Task<Result> Handle(RegisterExternalCustomerCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var externalCustomerRef = new ExternalCustomerRef
        {
            ExternalId = dto.ExternalId,
            DisplayName = dto.DisplayName,
            LegalName = dto.LegalName,
            RiskScore = dto.RiskScore,
            Metadata = dto.Metadata,
        };

        await repository.UpsertAsync(externalCustomerRef, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal sealed class RegisterExternalCustomerCommandValidator : AbstractValidator<RegisterExternalCustomerCommand>
{
    public RegisterExternalCustomerCommandValidator()
    {
        RuleFor(x => x.Dto.ExternalId)
            .NotEmpty().WithMessage("ExternalId is required.");
    }
}
