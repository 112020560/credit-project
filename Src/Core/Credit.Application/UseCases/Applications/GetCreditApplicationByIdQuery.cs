using Credit.Application.Abstractions.Messaging;
using Credit.Domain.Models;
using MediatR;
using SharedKernel;

namespace Credit.Application.Queries;

public record GetCreditApplicationByIdQuery(
    Guid CreditApplicationId
): IQuery<CreditApplicationModel>;


internal sealed class GetCreditApplicationByIdQueryHandler : IQueryHandler<GetCreditApplicationByIdQuery, CreditApplicationModel>
{
    public Task<Result<CreditApplicationModel>> Handle(GetCreditApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}