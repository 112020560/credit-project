using System;
using Credit.Application.Abstractions.Messaging;
using Credit.Domain.Models;
using SharedKernel;

namespace Credit.Application.Queries;

public record GetLinePaymentScheduleByIdQuery(Guid CreditLineId): IQuery<PaymentScheduleModel[]>;

internal class GetLinePaymentScheduleByIdQueryHandler : IQueryHandler<GetLinePaymentScheduleByIdQuery, PaymentScheduleModel[]>
{
    public Task<Result<PaymentScheduleModel[]>> Handle(GetLinePaymentScheduleByIdQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

