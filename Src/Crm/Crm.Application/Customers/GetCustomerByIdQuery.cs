using Crm.Application.Abstractions.Messaging;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Customers;
using SharedKernel;

namespace Crm.Application.Customers;

public record GetCustomerByIdQuery(Guid CustomerId): IQuery<CustomerModel>;

internal sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerModel>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCustomerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerModel>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _unitOfWork.CustomersRepository.GetCustomerByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerModel>(CustomerError.NotFound(request.CustomerId));
        }

        return new CustomerModel
        {
            Id = customer.Id,
            ExternalCode = customer.ExternalCode,
            IdentificationType = customer.IdentificationType,
            IdentificationNumber = customer.IdentificationNumber,
            FullName = customer.FullName,
            DisplayName = customer.DisplayName,
            BirthDate = customer.BirthDate,
            Status = customer.Status,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
        };
    }
}
