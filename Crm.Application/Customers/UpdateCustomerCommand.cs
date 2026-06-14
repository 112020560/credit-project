using Crm.Application.Abstractions.Messaging;
using Crm.Application.Abstractions.Mq;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using SharedKernel;
using SharedKernel.Contracts.Crm.Customers;

namespace Crm.Application.Customers;

public record UpdateCustomerCommand(Guid CustomerId, UpdateCustomerDto Dto) : ICommand<CustomerSummaryDto>;

internal sealed class UpdateCustomerCommandHandler(
    IUnitOfWork unitOfWork,
    IMqProducerService mqProducerService)
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
        customer.FullName = dto.FullName;
        customer.DisplayName = dto.DisplayName;
        customer.IdentificationType = dto.IdentificationType;
        customer.IdentificationNumber = dto.IdentificationNumber;
        customer.BirthDate = dto.BirthDate;
        customer.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.CustomersRepository.UpdateCustomerAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await mqProducerService.PublishEvent(
            new
            {
                CustomerId = customer.Id,
                UpdatedAt = DateTimeOffset.UtcNow,
                Version = 1,
                Changes = (IDictionary<string, object>)new Dictionary<string, object>
                {
                    ["FullName"] = customer.FullName,
                    ["DisplayName"] = customer.DisplayName ?? string.Empty,
                    ["IdentificationNumber"] = customer.IdentificationNumber ?? string.Empty
                }
            },
            Guid.NewGuid().ToString("N"),
            cancellationToken);

        return Result.Success(new CustomerSummaryDto(
            customer.Id, customer.FullName,
            customer.DisplayName ?? string.Empty,
            customer.IdentificationNumber ?? string.Empty));
    }
}
