using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using FluentValidation;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.Customers;

public record UpdateCustomerFinancialsCommand(Guid CustomerId, UpdateCustomerFinancialsDto Dto) : ICommand;

internal sealed class UpdateCustomerFinancialsCommandHandler(
    IUnitOfWork unitOfWork,
    IOutboxWriter outbox)
    : ICommandHandler<UpdateCustomerFinancialsCommand>
{
    public async Task<Result> Handle(UpdateCustomerFinancialsCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.CustomersRepository
            .GetForUpdateAsync(request.CustomerId, cancellationToken);

        if (customer is null)
            return Result.Failure(CustomerError.NotFound(request.CustomerId));

        var dto = request.Dto;
        customer.UpdateFinancials(dto.CreditScore, dto.MonthlyIncome, dto.MonthlyDebt);

        var changes = new Dictionary<string, object>();
        if (dto.CreditScore is not null) changes["CreditScore"] = dto.CreditScore;
        if (dto.MonthlyIncome is not null) changes["MonthlyIncome"] = dto.MonthlyIncome;
        if (dto.MonthlyDebt is not null) changes["MonthlyDebt"] = dto.MonthlyDebt;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await outbox.AppendAsync(new OutboxEvent
        {
            ServiceName      = "crm",
            AggregateId      = customer.Id,
            AggregateType    = "Customer",
            EventType        = "CustomerFinancialsUpdated",
            DeduplicationKey = $"CustomerFinancialsUpdated:{customer.Id}:{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Payload          = JsonSerializer.Serialize(new CustomerUpdatedContract(customer.Id, DateTimeOffset.UtcNow, 1, changes))
        }, cancellationToken);

        return Result.Success();
    }
}

internal sealed class UpdateCustomerFinancialsCommandValidator : AbstractValidator<UpdateCustomerFinancialsCommand>
{
    public UpdateCustomerFinancialsCommandValidator()
    {
        RuleFor(x => x.Dto)
            .Must(dto => dto.CreditScore is not null || dto.MonthlyIncome is not null || dto.MonthlyDebt is not null)
            .WithMessage("At least one financial field (CreditScore, MonthlyIncome, MonthlyDebt) must be provided.");
    }
}
