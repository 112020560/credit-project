using System;
using Credit.Application.Abstractions.Messaging;
using Credit.Domain.Models;
using SharedKernel;

namespace Credit.Application.Queries;

public record GetLinePaymentsByIdQuery(Guid CreditLineId): IQuery<PaymentModel[]>;

internal sealed class GetLinePaymentsByIdQueryHandler : IQueryHandler<GetLinePaymentsByIdQuery, PaymentModel[]>
{
    public Task<Result<PaymentModel[]>> Handle(GetLinePaymentsByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

