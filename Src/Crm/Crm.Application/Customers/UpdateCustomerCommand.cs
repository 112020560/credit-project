using System.Text.Json;
using Crm.Application.Abstractions.Messaging;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;
using SmartCore.Outbox.Abstractions;
using SmartCore.Outbox.Models;

namespace Crm.Application.Customers;

public record UpdateCustomerCommand(Guid CustomerId, UpdateCustomerDto Dto) : ICommand<CustomerSummaryDto>;

internal sealed class UpdateCustomerCommandHandler(
    IUnitOfWork unitOfWork,
    IOutboxWriter outbox)
    : ICommandHandler<UpdateCustomerCommand, CustomerSummaryDto>
{
    public async Task<Result<CustomerSummaryDto>> Handle(
        UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.CustomersRepository
            .GetCustomerByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
            return Result.Failure<CustomerSummaryDto>(CustomerError.NotFound(request.CustomerId));

        var dto = request.Dto;
        customer.UpdateProfile(dto.FullName, dto.DisplayName, dto.IdentificationType, dto.IdentificationNumber, dto.BirthDate);

        await unitOfWork.CustomersRepository.UpdateCustomerAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var contract = new CustomerUpdatedContract(
            customer.Id,
            DateTimeOffset.UtcNow,
            1,
            new Dictionary<string, object>
            {
                ["FullName"] = customer.FullName,
                ["DisplayName"] = customer.DisplayName ?? string.Empty,
                ["IdentificationNumber"] = customer.IdentificationNumber ?? string.Empty
            });

        await outbox.AppendAsync(new OutboxEvent
        {
            ServiceName      = "crm",
            AggregateId      = customer.Id,
            AggregateType    = "Customer",
            EventType        = "CustomerUpdated",
            DeduplicationKey = $"CustomerUpdated:{customer.Id}:{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Payload          = JsonSerializer.Serialize(contract)
        }, cancellationToken);

        return Result.Success(new CustomerSummaryDto(
            customer.Id, customer.FullName,
            customer.DisplayName ?? string.Empty,
            customer.IdentificationNumber ?? string.Empty));
    }
}
