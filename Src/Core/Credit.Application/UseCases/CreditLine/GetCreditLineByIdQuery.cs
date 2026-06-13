using System;
using Credit.Application.Abstractions.Messaging;
using Credit.Domain.Models;
using SharedKernel;

namespace Credit.Application.Queries;

public record GetCreditLineByIdQuery(Guid CreditLineId): IQuery<CreditLineModel>;

internal sealed class GetCreditLineByIdQueryHandler : IQueryHandler<GetCreditLineByIdQuery, CreditLineModel>
{
    public Task<Result<CreditLineModel>> Handle(GetCreditLineByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}