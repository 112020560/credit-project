using Crm.Application.Abstractions.Messaging;
using Crm.Application.Customers.Dtos;
using Crm.Domain.Abstractions.Persistence;
using SharedKernel;

namespace Crm.Application.Customers;

public record SearchCustomersQuery(string? Query, int Page, int PageSize) : IQuery<CustomerSearchPagedResponse>;

internal sealed class SearchCustomersQueryHandler(IUnitOfWork unitOfWork)
    : IQueryHandler<SearchCustomersQuery, CustomerSearchPagedResponse>
{
    public async Task<Result<CustomerSearchPagedResponse>> Handle(
        SearchCustomersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, total) = await unitOfWork.CustomersRepository
            .SearchAsync(request.Query, page, pageSize, cancellationToken);

        var dtos = items.Select(c => new CustomerSearchResultDto(
            c.Id,
            c.FullName,
            c.DisplayName,
            c.IdentificationType,
            c.IdentificationNumber,
            c.CustomerEmails.FirstOrDefault(e => e.IsPrimary == true)?.Email
                ?? c.CustomerEmails.FirstOrDefault()?.Email,
            c.CustomerPhones.FirstOrDefault(p => p.IsPrimary == true)?.Number
                ?? c.CustomerPhones.FirstOrDefault()?.Number,
            c.Status))
            .ToList();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Result.Success(new CustomerSearchPagedResponse(dtos, page, pageSize, total, totalPages));
    }
}
